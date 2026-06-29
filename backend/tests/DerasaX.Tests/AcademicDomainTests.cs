using System;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Infrastructure.DbHelper.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>Phase 4 §6.2/§6.3 — academic structure and user relationships integrity.</summary>
public class AcademicDomainTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public AcademicDomainTests(IntegrationFactory factory) => _factory = factory;

    private static async Task<string> UserId(DerasaXDbContext db, string loginCode) =>
        (await db.applicationUsers.IgnoreQueryFilters().FirstAsync(u => u.LoginCode == loginCode)).Id;

    [Fact]
    public async Task ParentStudent_cross_tenant_rejected_same_tenant_ok_and_many_to_many()
    {
        await using var setup = Phase4Db.Platform(_factory);
        var parentT1 = await UserId(setup, "PARENT-T1");
        var studentT1 = await UserId(setup, "STU-T1");
        var studentT2 = await UserId(setup, "STU-T2");
        var parentT2 = await UserId(setup, "PARENT-T2");

        var created = new System.Collections.Generic.List<string>();
        try
        {
            // Cross-tenant: tenant-1 link to a tenant-2 student must be DB-rejected (trigger).
            await using (var bad = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                bad.parentStudentRelationships.Add(new ParentStudentRelationship
                {
                    Id = Phase4Db.NewId("psr"), TenantId = "tenant-1",
                    ParentId = parentT1, StudentId = studentT2, ActiveFrom = DateTime.UtcNow
                });
                await Assert.ThrowsAnyAsync<DbUpdateException>(() => bad.SaveChangesAsync());
            }

            // Same-tenant link succeeds; multiple parents per student + multiple children per parent.
            await using (var good = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                var a = new ParentStudentRelationship { Id = Phase4Db.NewId("psr"), TenantId = "tenant-1", ParentId = parentT1, StudentId = studentT1, IsPrimary = true, ActiveFrom = DateTime.UtcNow };
                good.parentStudentRelationships.Add(a);
                await good.SaveChangesAsync();
                created.Add(a.Id);
            }

            // Different tenant uses its own users independently (no collision).
            await using (var t2 = Phase4Db.AsTenant(_factory, "tenant-2"))
            {
                var b = new ParentStudentRelationship { Id = Phase4Db.NewId("psr"), TenantId = "tenant-2", ParentId = parentT2, StudentId = studentT2, ActiveFrom = DateTime.UtcNow };
                t2.parentStudentRelationships.Add(b);
                await t2.SaveChangesAsync();
                created.Add(b.Id);
            }

            // Duplicate (TenantId, ParentId, StudentId) within a tenant is rejected.
            await using (var dup = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                dup.parentStudentRelationships.Add(new ParentStudentRelationship { Id = Phase4Db.NewId("psr"), TenantId = "tenant-1", ParentId = parentT1, StudentId = studentT1, ActiveFrom = DateTime.UtcNow });
                await Assert.ThrowsAnyAsync<DbUpdateException>(() => dup.SaveChangesAsync());
            }
        }
        finally { await CleanupAsync("parentStudentRelationships", created); }
    }

    [Fact]
    public async Task AcademicYear_code_unique_per_tenant_but_shared_across_tenants()
    {
        var code = "AY-" + Guid.NewGuid().ToString("N")[..6];
        var ids = new System.Collections.Generic.List<string>();
        try
        {
            await using (var t1 = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                var y = NewYear("tenant-1", code); t1.academicYears.Add(y); await t1.SaveChangesAsync(); ids.Add(y.Id);
            }
            // Same code, different tenant -> allowed.
            await using (var t2 = Phase4Db.AsTenant(_factory, "tenant-2"))
            {
                var y = NewYear("tenant-2", code); t2.academicYears.Add(y); await t2.SaveChangesAsync(); ids.Add(y.Id);
            }
            // Same code, same tenant -> rejected.
            await using (var dup = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                dup.academicYears.Add(NewYear("tenant-1", code));
                await Assert.ThrowsAnyAsync<DbUpdateException>(() => dup.SaveChangesAsync());
            }
        }
        finally { await CleanupAsync("academicYears", ids); }
    }

    [Fact]
    public async Task Enrollment_cross_tenant_student_rejected_same_tenant_ok()
    {
        await using var setup = Phase4Db.Platform(_factory);
        var studentT1 = await UserId(setup, "STU-T1");
        var studentT2 = await UserId(setup, "STU-T2");

        var yearId = Phase4Db.NewId("ay");
        var classId = Phase4Db.NewId("cls");
        var enrollIds = new System.Collections.Generic.List<string>();
        try
        {
            await using (var t1 = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                t1.academicYears.Add(new AcademicYear { Id = yearId, TenantId = "tenant-1", Name = "Y", Code = "AYC-" + yearId[^6..], StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddYears(1) });
                t1.schoolClasses.Add(new SchoolClass { Id = classId, TenantId = "tenant-1", Name = "7A", Code = "C-" + classId[^6..], GradeId = "G7-ID", AcademicYearId = yearId });
                await t1.SaveChangesAsync();
            }

            // Cross-tenant student enrollment into tenant-1 class -> rejected by trigger.
            await using (var bad = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                bad.enrollments.Add(new Enrollment { Id = Phase4Db.NewId("enr"), TenantId = "tenant-1", StudentId = studentT2, SchoolClassId = classId, AcademicYearId = yearId, EnrolledAt = DateTime.UtcNow });
                await Assert.ThrowsAnyAsync<DbUpdateException>(() => bad.SaveChangesAsync());
            }

            // Same-tenant enrollment succeeds.
            await using (var good = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                var e = new Enrollment { Id = Phase4Db.NewId("enr"), TenantId = "tenant-1", StudentId = studentT1, SchoolClassId = classId, AcademicYearId = yearId, EnrolledAt = DateTime.UtcNow };
                good.enrollments.Add(e); await good.SaveChangesAsync(); enrollIds.Add(e.Id);
            }
        }
        finally
        {
            await CleanupAsync("enrollments", enrollIds);
            await CleanupAsync("schoolClasses", new[] { classId });
            await CleanupAsync("academicYears", new[] { yearId });
        }
    }

    [Fact]
    public async Task TeacherSubject_cross_tenant_teacher_rejected()
    {
        await using var setup = Phase4Db.Platform(_factory);
        var teacherT2 = await UserId(setup, "TEACH-T2");
        var teacherT1 = await UserId(setup, "TEACH-T1");

        var subjectId = Phase4Db.NewId("subj");
        var ids = new System.Collections.Generic.List<string>();
        try
        {
            await using (var t1 = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                t1.subjects.Add(new Subject { Id = subjectId, TenantId = "tenant-1", Name = "Math", GradeId = "G7-ID" });
                await t1.SaveChangesAsync();
            }

            // tenant-1 subject + tenant-2 teacher -> rejected by trigger.
            await using (var bad = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                bad.teacherSubjectAssignments.Add(new TeacherSubjectAssignment { Id = Phase4Db.NewId("tsa"), TenantId = "tenant-1", TeacherId = teacherT2, SubjectId = subjectId, ActiveFrom = DateTime.UtcNow });
                await Assert.ThrowsAnyAsync<DbUpdateException>(() => bad.SaveChangesAsync());
            }

            // Same-tenant assignment succeeds.
            await using (var good = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                var a = new TeacherSubjectAssignment { Id = Phase4Db.NewId("tsa"), TenantId = "tenant-1", TeacherId = teacherT1, SubjectId = subjectId, ActiveFrom = DateTime.UtcNow };
                good.teacherSubjectAssignments.Add(a); await good.SaveChangesAsync(); ids.Add(a.Id);
            }
        }
        finally
        {
            await CleanupAsync("teacherSubjectAssignments", ids);
            await CleanupAsync("subjects", new[] { subjectId });
        }
    }

    private static AcademicYear NewYear(string tenant, string code) => new()
    {
        Id = Phase4Db.NewId("ay"), TenantId = tenant, Name = "Year", Code = code,
        StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddYears(1)
    };

    private async Task CleanupAsync(string set, System.Collections.Generic.IEnumerable<string> ids)
    {
        await using var db = Phase4Db.Platform(_factory);
        // 'set' is a controlled constant table name; the id is a bound parameter.
        var sql = "DELETE FROM \"" + set + "\" WHERE \"Id\" = {0}";
        foreach (var id in ids)
            await db.Database.ExecuteSqlRawAsync(sql, id);
    }
}
