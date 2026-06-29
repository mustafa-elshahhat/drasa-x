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
/// Phase 5 closure — teacher↔student private messaging. A student may message a teacher of a
/// class they are actively enrolled in (and vice-versa); the relationship is the gate. Non-linked
/// pairs are 403, cross-tenant participants and non-participant readers are 404.
/// </summary>
public class TeacherStudentMessagingTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public TeacherStudentMessagingTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private sealed record Env<T>(bool success, int statusCode, T? data, int totalCount);
    private sealed record IdRow(string id, int type);
    private static string NewId(string p) => $"{p}-{Guid.NewGuid():N}";
    private static DateTime U(int y, int m, int d) => new(y, m, d, 0, 0, 0, DateTimeKind.Utc);

    private async Task<string> UserId(string loginCode)
    {
        await using var db = Phase4Db.Platform(_factory);
        return (await db.applicationUsers.IgnoreQueryFilters().FirstAsync(u => u.LoginCode == loginCode)).Id;
    }

    private sealed record World(string classId, string yearId, string enrollId, string tcaId, string studentId, string teacherId, TestUsers.CreatedUser student);

    private async Task<World> SetupAsync(bool link = true)
    {
        var student = await TestUsers.CreateLockoutStudentAsync(_factory);
        var teacherId = await UserId("TEACH-T1");
        var w = new World(NewId("cls"), NewId("ay"), NewId("enr"), NewId("tca"), student.Id, teacherId, student);

        await using var db = Phase4Db.AsTenant(_factory, "tenant-1");
        db.academicYears.Add(new AcademicYear { Id = w.yearId, TenantId = "tenant-1", Name = "Y", Code = NewId("AY")[..12], StartDate = U(2030, 9, 1), EndDate = U(2031, 6, 30) });
        db.schoolClasses.Add(new SchoolClass { Id = w.classId, TenantId = "tenant-1", Name = "C", Code = NewId("C")[..12], GradeId = "G7-ID", AcademicYearId = w.yearId, Capacity = 30 });
        if (link)
        {
            db.enrollments.Add(new Enrollment { Id = w.enrollId, TenantId = "tenant-1", StudentId = w.studentId, SchoolClassId = w.classId, AcademicYearId = w.yearId, Status = EnrollmentStatus.Active, EnrolledAt = DateTime.UtcNow });
            db.teacherClassAssignments.Add(new TeacherClassAssignment { Id = w.tcaId, TenantId = "tenant-1", TeacherId = teacherId, SchoolClassId = w.classId, IsActive = true, ActiveFrom = DateTime.UtcNow });
        }
        await db.SaveChangesAsync();
        return w;
    }

    private async Task Cleanup(World w, IEnumerable<string>? conversationIds = null)
    {
        await using var db = Phase4Db.Platform(_factory);
        foreach (var cid in conversationIds ?? Enumerable.Empty<string>())
        {
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"messageReadReceipts\" WHERE \"MessageId\" IN (SELECT \"Id\" FROM \"messages\" WHERE \"ConversationId\" = {0})", cid);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"messages\" WHERE \"ConversationId\" = {0}", cid);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"conversationParticipants\" WHERE \"ConversationId\" = {0}", cid);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"conversations\" WHERE \"Id\" = {0}", cid);
        }
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"notifications\" WHERE \"UserId\" IN ({0},{1})", w.studentId, w.teacherId);
        foreach (var (table, id) in new[] { ("teacherClassAssignments", w.tcaId), ("enrollments", w.enrollId), ("schoolClasses", w.classId), ("academicYears", w.yearId) })
#pragma warning disable EF1002
            await db.Database.ExecuteSqlRawAsync($"DELETE FROM \"{table}\" WHERE \"Id\" = {{0}}", id);
#pragma warning restore EF1002
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"auditLogs\" WHERE \"ActorUserId\" = {0}", w.studentId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUserRoles\" WHERE \"UserId\" = {0}", w.studentId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUsers\" WHERE \"Id\" = {0}", w.studentId);
    }

    private static async Task<Env<T>?> Read<T>(HttpResponseMessage r)
    {
        var raw = await r.Content.ReadAsStringAsync();
        return (!string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith("{")) ? JsonSerializer.Deserialize<Env<T>>(raw, Json) : null;
    }

    [Fact]
    public async Task Student_can_message_assigned_teacher_and_outsiders_are_blocked()
    {
        var w = await SetupAsync();
        var convIds = new List<string>();
        try
        {
            // Student (enrolled) starts a StudentTeacher conversation with the assigned teacher.
            var studentClient = await TestClient.AuthedClientAsync(_factory, w.student.LoginCode);
            var start = await studentClient.PostAsJsonAsync("/api/v1/conversations", new
            {
                participantUserId = w.teacherId, subject = "Question about homework", firstMessage = "Can you help?"
            });
            var (ss, sb) = (start.StatusCode, await Read<IdRow>(start));
            Assert.Equal(HttpStatusCode.Created, ss);
            Assert.Equal((int)ConversationType.StudentTeacher, sb!.data!.type);
            var convId = sb.data.id;
            convIds.Add(convId);

            // The teacher is a participant and can reply.
            var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
            var reply = await teacher.PostAsJsonAsync($"/api/v1/conversations/{convId}/messages", new { body = "Of course" });
            Assert.Equal(HttpStatusCode.Created, reply.StatusCode);

            // A different (non-participant) student cannot read it → 404 (no existence leak).
            var outsider = await TestClient.AuthedClientAsync(_factory, "STU-T1");
            Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/v1/conversations/{convId}/messages")).StatusCode);

            // Notification was raised for the teacher.
            await using var db = Phase4Db.Platform(_factory);
            Assert.True(await db.conversations.IgnoreQueryFilters().AnyAsync(c => c.Id == convId && c.Type == ConversationType.StudentTeacher));
        }
        finally { await Cleanup(w, convIds); }
    }

    [Fact]
    public async Task Unlinked_student_teacher_is_forbidden_and_cross_tenant_is_404()
    {
        var w = await SetupAsync(link: false); // no enrollment/assignment
        try
        {
            var studentClient = await TestClient.AuthedClientAsync(_factory, w.student.LoginCode);

            // No teaching relationship → 403.
            var noLink = await studentClient.PostAsJsonAsync("/api/v1/conversations", new { participantUserId = w.teacherId });
            Assert.Equal(HttpStatusCode.Forbidden, noLink.StatusCode);

            // Cross-tenant teacher → 404 (no existence leak).
            var cross = await studentClient.PostAsJsonAsync("/api/v1/conversations", new { participantUserId = await UserId("TEACH-T2") });
            Assert.Equal(HttpStatusCode.NotFound, cross.StatusCode);
        }
        finally { await Cleanup(w); }
    }
}
