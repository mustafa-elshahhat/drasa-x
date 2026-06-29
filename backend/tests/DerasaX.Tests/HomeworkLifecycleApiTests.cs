using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 5 closure — end-to-end homework (non-quiz assignment) lifecycle: teacher draft → publish
/// to a class → student assigned-listing → submit (idempotent: duplicate 409) → teacher review →
/// grade + feedback → student sees the grade. Non-assigned students are blocked; cross-tenant 404.
/// </summary>
public class HomeworkLifecycleApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public HomeworkLifecycleApiTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private sealed record Env<T>(bool success, int statusCode, T? data, int totalCount);
    private sealed record HwRow(string id, int status, int type);
    private sealed record AssignedRow(string assignmentId, string title, bool hasSubmitted, int? submissionStatus, decimal? score);
    private sealed record SubRow(string id, string studentId, int status, decimal? score);
    private static string NewId(string p) => $"{p}-{Guid.NewGuid():N}";
    private static DateTime U(int y, int m, int d) => new(y, m, d, 0, 0, 0, DateTimeKind.Utc);

    private async Task<string> UserId(string loginCode)
    {
        await using var db = Phase4Db.Platform(_factory);
        return (await db.applicationUsers.IgnoreQueryFilters().FirstAsync(u => u.LoginCode == loginCode)).Id;
    }

    private sealed record World(string classId, string yearId, string enrollId, string tcaId, string teacherId, TestUsers.CreatedUser student);

    private async Task<World> SetupAsync()
    {
        var student = await TestUsers.CreateLockoutStudentAsync(_factory);
        var teacherId = await UserId("TEACH-T1");
        var w = new World(NewId("cls"), NewId("ay"), NewId("enr"), NewId("tca"), teacherId, student);

        await using var db = Phase4Db.AsTenant(_factory, "tenant-1");
        db.academicYears.Add(new AcademicYear { Id = w.yearId, TenantId = "tenant-1", Name = "Y", Code = NewId("AY")[..12], StartDate = U(2030, 9, 1), EndDate = U(2031, 6, 30) });
        db.schoolClasses.Add(new SchoolClass { Id = w.classId, TenantId = "tenant-1", Name = "C", Code = NewId("C")[..12], GradeId = "G7-ID", AcademicYearId = w.yearId, Capacity = 30 });
        db.enrollments.Add(new Enrollment { Id = w.enrollId, TenantId = "tenant-1", StudentId = w.student.Id, SchoolClassId = w.classId, AcademicYearId = w.yearId, Status = EnrollmentStatus.Active, EnrolledAt = DateTime.UtcNow });
        db.teacherClassAssignments.Add(new TeacherClassAssignment { Id = w.tcaId, TenantId = "tenant-1", TeacherId = teacherId, SchoolClassId = w.classId, IsActive = true, ActiveFrom = DateTime.UtcNow });
        await db.SaveChangesAsync();
        return w;
    }

    private async Task Cleanup(World w, IEnumerable<string> assignmentIds)
    {
        await using var db = Phase4Db.Platform(_factory);
        foreach (var aid in assignmentIds)
        {
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"assignmentSubmissions\" WHERE \"AssignmentId\" = {0}", aid);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"assignmentTargets\" WHERE \"AssignmentId\" = {0}", aid);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"assignments\" WHERE \"Id\" = {0}", aid);
        }
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"notifications\" WHERE \"UserId\" IN ({0},{1})", w.student.Id, w.teacherId);
        foreach (var (table, id) in new[] { ("teacherClassAssignments", w.tcaId), ("enrollments", w.enrollId), ("schoolClasses", w.classId), ("academicYears", w.yearId) })
#pragma warning disable EF1002
            await db.Database.ExecuteSqlRawAsync($"DELETE FROM \"{table}\" WHERE \"Id\" = {{0}}", id);
#pragma warning restore EF1002
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"auditLogs\" WHERE \"ActorUserId\" = {0}", w.student.Id);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUserRoles\" WHERE \"UserId\" = {0}", w.student.Id);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUsers\" WHERE \"Id\" = {0}", w.student.Id);
    }

    private static async Task<Env<T>?> Read<T>(HttpResponseMessage r)
    {
        var raw = await r.Content.ReadAsStringAsync();
        return (!string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith("{")) ? JsonSerializer.Deserialize<Env<T>>(raw, Json) : null;
    }

    [Fact]
    public async Task Full_homework_lifecycle_draft_publish_submit_grade()
    {
        var w = await SetupAsync();
        var assignmentIds = new List<string>();
        try
        {
            var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");

            // 1. Create draft.
            var create = await teacher.PostAsJsonAsync("/api/v1/homework", new
            {
                title = "Essay on Photosynthesis", description = "300 words", type = "Homework", maxScore = 100
            });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var hw = (await Read<HwRow>(create))!.data!;
            assignmentIds.Add(hw.id);
            Assert.Equal((int)AssignmentStatus.Draft, hw.status);
            Assert.Equal((int)AssignmentType.Homework, hw.type);

            // 2. Publish to the class.
            var publish = await teacher.PostAsJsonAsync($"/api/v1/homework/{hw.id}/publish", new { schoolClassId = w.classId });
            Assert.Equal(HttpStatusCode.OK, publish.StatusCode);
            Assert.Equal((int)AssignmentStatus.Published, (await Read<HwRow>(publish))!.data!.status);

            // 3. Student sees it in assigned list.
            var studentClient = await TestClient.AuthedClientAsync(_factory, w.student.LoginCode);
            var assigned = await Read<List<AssignedRow>>(await studentClient.GetAsync("/api/v1/homework/assigned"));
            Assert.Contains(assigned!.data!, a => a.assignmentId == hw.id && !a.hasSubmitted);

            // 4. Student submits; a duplicate submit → 409.
            var submit = await studentClient.PostAsJsonAsync($"/api/v1/homework/{hw.id}/submit", new { content = "My essay..." });
            Assert.Equal(HttpStatusCode.Created, submit.StatusCode);
            var submissionId = (await Read<SubRow>(submit))!.data!.id;
            Assert.Equal(HttpStatusCode.Conflict, (await studentClient.PostAsJsonAsync($"/api/v1/homework/{hw.id}/submit", new { content = "again" })).StatusCode);

            // 5. Teacher lists submissions and grades.
            var subs = await Read<List<SubRow>>(await teacher.GetAsync($"/api/v1/homework/{hw.id}/submissions?pageSize=50"));
            Assert.True(subs!.totalCount >= 1);
            var grade = await teacher.PostAsJsonAsync($"/api/v1/homework/submissions/{submissionId}/grade", new { score = 88, feedback = "Well done" });
            Assert.Equal(HttpStatusCode.OK, grade.StatusCode);

            // 6. Student sees the grade.
            var mine = await Read<SubRow>(await studentClient.GetAsync($"/api/v1/homework/{hw.id}/my-submission"));
            Assert.Equal((int)SubmissionStatus.Graded, mine!.data!.status);
            Assert.Equal(88, mine.data.score);

            // 7. Cross-tenant teacher cannot see this homework → 404.
            var t2 = await TestClient.AuthedClientAsync(_factory, "TEACH-T2");
            Assert.Equal(HttpStatusCode.NotFound, (await t2.GetAsync($"/api/v1/homework/{hw.id}")).StatusCode);
        }
        finally { await Cleanup(w, assignmentIds); }
    }

    [Fact]
    public async Task Non_assigned_student_cannot_submit()
    {
        var w = await SetupAsync();
        var assignmentIds = new List<string>();
        try
        {
            var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
            var create = await teacher.PostAsJsonAsync("/api/v1/homework", new { title = "Reading", type = "Reading" });
            var hw = (await Read<HwRow>(create))!.data!;
            assignmentIds.Add(hw.id);
            await teacher.PostAsJsonAsync($"/api/v1/homework/{hw.id}/publish", new { schoolClassId = w.classId });

            // STU-T1 (not enrolled in this class) is not a target → 403.
            var outsider = await TestClient.AuthedClientAsync(_factory, "STU-T1");
            var submit = await outsider.PostAsJsonAsync($"/api/v1/homework/{hw.id}/submit", new { content = "x" });
            Assert.Equal(HttpStatusCode.Forbidden, submit.StatusCode);
        }
        finally { await Cleanup(w, assignmentIds); }
    }
}
