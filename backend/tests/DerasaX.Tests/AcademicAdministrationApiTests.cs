using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Infrastructure.DbHelper.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 5 §9.1 — academic administration API: authentication, role authorization,
/// tenant isolation, validation, conflict handling, and the create→enroll→withdraw
/// lifecycle, exercised through the real HTTP pipeline against local PostgreSQL.
/// Cross-tenant enroll/assign cases give API-level coverage of the same-tenant
/// database triggers (Phase 5 §13).
/// </summary>
public class AcademicAdministrationApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public AcademicAdministrationApiTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private sealed record Envelope<T>(bool success, int statusCode, string? message, T? data, int pageSize, int totalCount);
    private sealed record IdData(string id, string code);
    // EnrollmentStatus serializes as its integer value (Active=0, Withdrawn=1).
    private sealed record EnrollData(string id, string studentId, int status);

    private static string Code(string p) => $"{p}-{Guid.NewGuid():N}"[..14];

    private async Task<HttpClient> Admin1() => await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");

    private static async Task<(HttpStatusCode status, Envelope<IdData>? body)> CreateYear(HttpClient c, string? code = null)
    {
        var resp = await c.PostAsJsonAsync("/api/v1/academic-years", new
        {
            name = "Year " + code,
            code = code ?? Code("AY"),
            startDate = new DateTime(2030, 9, 1),
            endDate = new DateTime(2031, 6, 30),
            isCurrent = false
        });
        Envelope<IdData>? body = null;
        var raw = await resp.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith("{"))
            try { body = JsonSerializer.Deserialize<Envelope<IdData>>(raw, Json); } catch { /* problem+json */ }
        return (resp.StatusCode, body);
    }

    private async Task Cleanup(string set, IEnumerable<string> ids)
    {
        await using var db = Phase4Db.Platform(_factory);
        foreach (var id in ids)
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"" + set + "\" WHERE \"Id\" = {0}", id);
    }

    // ---- Authentication & role authorization ----

    [Fact]
    public async Task Unauthenticated_list_returns_401()
    {
        var client = TestClient.NewClient(_factory);
        var resp = await client.GetAsync("/api/v1/academic-years");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task TenantMember_student_can_list()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var resp = await client.GetAsync("/api/v1/academic-years");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Student_cannot_create_year_403()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var (status, _) = await CreateYear(client);
        Assert.Equal(HttpStatusCode.Forbidden, status);
    }

    [Fact]
    public async Task Teacher_cannot_create_year_403()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
        var (status, _) = await CreateYear(client);
        Assert.Equal(HttpStatusCode.Forbidden, status);
    }

    [Fact]
    public async Task SchoolAdmin_can_create_year_201()
    {
        var client = await Admin1();
        var (status, body) = await CreateYear(client);
        Assert.Equal(HttpStatusCode.Created, status);
        Assert.NotNull(body);
        Assert.True(body!.success);
        Assert.False(string.IsNullOrEmpty(body.data!.id));
        await Cleanup("academicYears", new[] { body.data.id });
    }

    [Fact]
    public async Task Platform_systemadmin_cannot_use_tenant_route_403()
    {
        var client = await TestClient.AuthedClientAsync(_factory, "SYS-1");
        var resp = await client.GetAsync("/api/v1/academic-years");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ---- Validation & conflict ----

    [Fact]
    public async Task Create_year_with_invalid_dates_returns_400()
    {
        var client = await Admin1();
        var resp = await client.PostAsJsonAsync("/api/v1/academic-years", new
        {
            name = "Bad", code = Code("AY"),
            startDate = new DateTime(2031, 6, 30), endDate = new DateTime(2030, 9, 1)
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Create_duplicate_year_code_returns_409()
    {
        var client = await Admin1();
        var code = Code("AY");
        var (s1, b1) = await CreateYear(client, code);
        Assert.Equal(HttpStatusCode.Created, s1);
        var (s2, _) = await CreateYear(client, code);
        Assert.Equal(HttpStatusCode.Conflict, s2);
        await Cleanup("academicYears", new[] { b1!.data!.id });
    }

    [Fact]
    public async Task List_page_size_is_clamped_to_max()
    {
        var client = await Admin1();
        var resp = await client.GetAsync("/api/v1/academic-years?pageSize=500");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = JsonSerializer.Deserialize<Envelope<List<IdData>>>(await resp.Content.ReadAsStringAsync(), Json);
        Assert.True(body!.pageSize <= 100, $"pageSize was {body.pageSize}");
    }

    // ---- Tenant isolation (no cross-tenant existence leak: 404, not 403/500) ----

    [Fact]
    public async Task CrossTenant_read_update_archive_return_404()
    {
        var admin1 = await Admin1();
        var (status, body) = await CreateYear(admin1);
        Assert.Equal(HttpStatusCode.Created, status);
        var id = body!.data!.id;
        try
        {
            var admin2 = await TestClient.AuthedClientAsync(_factory, "ADMIN-T2");

            var read = await admin2.GetAsync($"/api/v1/academic-years/{id}");
            Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);

            var update = await admin2.PutAsJsonAsync($"/api/v1/academic-years/{id}", new
            {
                name = "Hijack", code = Code("AY"),
                startDate = new DateTime(2030, 9, 1), endDate = new DateTime(2031, 6, 30)
            });
            Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);

            var archive = await admin2.DeleteAsync($"/api/v1/academic-years/{id}");
            Assert.Equal(HttpStatusCode.NotFound, archive.StatusCode);

            // Owner still sees it intact.
            var owner = await admin1.GetAsync($"/api/v1/academic-years/{id}");
            Assert.Equal(HttpStatusCode.OK, owner.StatusCode);
        }
        finally { await Cleanup("academicYears", new[] { id }); }
    }

    // ---- Lifecycle: year -> class -> enroll -> withdraw ----

    [Fact]
    public async Task Full_enrollment_lifecycle_and_cross_tenant_rejection()
    {
        var admin1 = await Admin1();
        string yearId = "", classId = "", enrollId = "";
        try
        {
            var (ys, yb) = await CreateYear(admin1);
            Assert.Equal(HttpStatusCode.Created, ys);
            yearId = yb!.data!.id;

            var classResp = await admin1.PostAsJsonAsync("/api/v1/classes", new
            {
                name = "7-A", code = Code("C"), capacity = 30, gradeId = "G7-ID", academicYearId = yearId
            });
            Assert.Equal(HttpStatusCode.Created, classResp.StatusCode);
            classId = JsonSerializer.Deserialize<Envelope<IdData>>(await classResp.Content.ReadAsStringAsync(), Json)!.data!.id;

            // Enroll a same-tenant student -> 201.
            var enrResp = await admin1.PostAsJsonAsync("/api/v1/enrollments", new
            {
                studentId = await UserId("STU-T1"), schoolClassId = classId
            });
            Assert.Equal(HttpStatusCode.Created, enrResp.StatusCode);
            var enr = JsonSerializer.Deserialize<Envelope<EnrollData>>(await enrResp.Content.ReadAsStringAsync(), Json)!;
            enrollId = enr.data!.id;
            Assert.Equal((int)DerasaX.Domain.Enums.EnrollmentStatus.Active, enr.data.status);

            // Duplicate active enrollment -> 409.
            var dup = await admin1.PostAsJsonAsync("/api/v1/enrollments", new
            {
                studentId = await UserId("STU-T1"), schoolClassId = classId
            });
            Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);

            // Cross-tenant student (tenant-2) into tenant-1 class -> clean 404, never 500.
            var cross = await admin1.PostAsJsonAsync("/api/v1/enrollments", new
            {
                studentId = await UserId("STU-T2"), schoolClassId = classId
            });
            Assert.Equal(HttpStatusCode.NotFound, cross.StatusCode);

            // A notification record was created for the enrolled student (same transaction).
            await using (var db = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                var stuId = await UserId("STU-T1");
                var hasNotif = await db.notifications.AnyAsync(n => n.UserId == stuId && n.Title == "Enrolled in a class");
                Assert.True(hasNotif);
            }

            // Withdraw -> 200, status flips to Withdrawn.
            var wd = await admin1.PostAsJsonAsync($"/api/v1/enrollments/{enrollId}/withdraw", new { reason = "moved" });
            Assert.Equal(HttpStatusCode.OK, wd.StatusCode);
            var wdBody = JsonSerializer.Deserialize<Envelope<EnrollData>>(await wd.Content.ReadAsStringAsync(), Json)!;
            Assert.Equal((int)DerasaX.Domain.Enums.EnrollmentStatus.Withdrawn, wdBody.data!.status);
        }
        finally
        {
            await Cleanup("notifications", await NotificationIds("Enrolled in a class"));
            if (enrollId != "") await Cleanup("enrollments", new[] { enrollId });
            if (classId != "") await Cleanup("schoolClasses", new[] { classId });
            if (yearId != "") await Cleanup("academicYears", new[] { yearId });
        }
    }

    [Fact]
    public async Task TeacherAssignment_same_tenant_ok_cross_tenant_rejected()
    {
        var admin1 = await Admin1();
        var subjectId = Phase4Db.NewId("subj");
        string assignId = "";
        try
        {
            await using (var db = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                db.subjects.Add(new Subject { Id = subjectId, TenantId = "tenant-1", Name = "Math", GradeId = "G7-ID" });
                await db.SaveChangesAsync();
            }

            // Same-tenant teacher -> 201.
            var ok = await admin1.PostAsJsonAsync("/api/v1/teacher-subject-assignments", new
            {
                teacherId = await UserId("TEACH-T1"), subjectId
            });
            Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
            assignId = JsonSerializer.Deserialize<Envelope<IdData>>(await ok.Content.ReadAsStringAsync(), Json)!.data!.id;

            // Cross-tenant teacher (tenant-2) -> clean 404.
            var cross = await admin1.PostAsJsonAsync("/api/v1/teacher-subject-assignments", new
            {
                teacherId = await UserId("TEACH-T2"), subjectId
            });
            Assert.Equal(HttpStatusCode.NotFound, cross.StatusCode);
        }
        finally
        {
            if (assignId != "") await Cleanup("teacherSubjectAssignments", new[] { assignId });
            await Cleanup("subjects", new[] { subjectId });
        }
    }

    // ---- helpers ----

    private async Task<string> UserId(string loginCode)
    {
        await using var db = Phase4Db.Platform(_factory);
        return (await db.applicationUsers.IgnoreQueryFilters().FirstAsync(u => u.LoginCode == loginCode)).Id;
    }

    private async Task<List<string>> NotificationIds(string title)
    {
        await using var db = Phase4Db.Platform(_factory);
        return await db.notifications.IgnoreQueryFilters().Where(n => n.Title == title).Select(n => n.Id).ToListAsync();
    }
}
