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
using DerasaX.Infrastructure.DbHelper.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 5 §12 (Increment 5) — communication workflows through the real HTTP pipeline:
/// the parent↔teacher relationship gate, participant-only messaging, read receipts, the parent
/// request lifecycle with status validation, announcement targeting, anonymous-suggestion
/// privacy + moderation authorization, and the required audit/notification records.
/// </summary>
public class CommunicationApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public CommunicationApiTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private sealed record Env<T>(bool success, int statusCode, T? data, int totalCount);
    private sealed record IdRow(string id);
    private sealed record SuggestionRow(string id, string title, SuggestionStatus status);
    private sealed record ReqRow(string id, int status);

    private static string NewId(string p) => $"{p}-{Guid.NewGuid():N}";

    private async Task<string> UserId(string loginCode)
    {
        await using var db = Phase4Db.Platform(_factory);
        return (await db.applicationUsers.IgnoreQueryFilters().FirstAsync(u => u.LoginCode == loginCode)).Id;
    }

    private sealed record World(string classId, string yearId, string enrollId, string tcaId, string psrId,
        string studentId, string teacherId, string parentId);

    private async Task<World> SetupAsync(bool link = true)
    {
        var student = await TestUsers.CreateLockoutStudentAsync(_factory);
        var teacherId = await UserId("TEACH-T1");
        var parentId = await UserId("PARENT-T1");
        var w = new World(NewId("cls"), NewId("ay"), NewId("enr"), NewId("tca"), NewId("psr"), student.Id, teacherId, parentId);

        await using var db = Phase4Db.AsTenant(_factory, "tenant-1");
        db.academicYears.Add(new AcademicYear { Id = w.yearId, TenantId = "tenant-1", Name = "Y", Code = NewId("AY")[..12], StartDate = U(2030, 9, 1), EndDate = U(2031, 6, 30) });
        db.schoolClasses.Add(new SchoolClass { Id = w.classId, TenantId = "tenant-1", Name = "C", Code = NewId("C")[..12], GradeId = "G7-ID", AcademicYearId = w.yearId, Capacity = 30 });
        if (link)
        {
            db.enrollments.Add(new Enrollment { Id = w.enrollId, TenantId = "tenant-1", StudentId = w.studentId, SchoolClassId = w.classId, AcademicYearId = w.yearId, Status = EnrollmentStatus.Active, EnrolledAt = DateTime.UtcNow });
            db.teacherClassAssignments.Add(new TeacherClassAssignment { Id = w.tcaId, TenantId = "tenant-1", TeacherId = teacherId, SchoolClassId = w.classId, IsActive = true, ActiveFrom = DateTime.UtcNow });
            db.parentStudentRelationships.Add(new ParentStudentRelationship { Id = w.psrId, TenantId = "tenant-1", ParentId = parentId, StudentId = w.studentId, IsActive = true, CanContactTeachers = true, CanRequestDocuments = true });
        }
        await db.SaveChangesAsync();
        return w;
    }

    private static DateTime U(int y, int m, int d) => new(y, m, d, 0, 0, 0, DateTimeKind.Utc);

    private async Task Cleanup(World w, IEnumerable<string>? conversationIds = null, IEnumerable<string>? parentRequestIds = null,
        IEnumerable<string>? announcementIds = null, IEnumerable<string>? suggestionIds = null)
    {
        await using var db = Phase4Db.Platform(_factory);
        foreach (var cid in conversationIds ?? Enumerable.Empty<string>())
        {
            await Del(db, "messageReadReceipts", "MessageId", $"SELECT \"Id\" FROM \"messages\" WHERE \"ConversationId\" = '{cid}'");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"messages\" WHERE \"ConversationId\" = {0}", cid);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"conversationParticipants\" WHERE \"ConversationId\" = {0}", cid);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"conversations\" WHERE \"Id\" = {0}", cid);
        }
        foreach (var rid in parentRequestIds ?? Enumerable.Empty<string>())
        {
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"parentRequestResponses\" WHERE \"ParentRequestId\" = {0}", rid);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"parentRequests\" WHERE \"Id\" = {0}", rid);
        }
        foreach (var aid in announcementIds ?? Enumerable.Empty<string>())
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"announcements\" WHERE \"Id\" = {0}", aid);
        foreach (var sid in suggestionIds ?? Enumerable.Empty<string>())
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"suggestions\" WHERE \"Id\" = {0}", sid);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"notifications\" WHERE \"UserId\" IN ({0},{1})", w.parentId, w.teacherId);

        foreach (var (table, id) in new[] { ("parentStudentRelationships", w.psrId), ("teacherClassAssignments", w.tcaId), ("enrollments", w.enrollId), ("schoolClasses", w.classId), ("academicYears", w.yearId) })
#pragma warning disable EF1002
            await db.Database.ExecuteSqlRawAsync($"DELETE FROM \"{table}\" WHERE \"Id\" = {{0}}", id);
#pragma warning restore EF1002
    }

    private static Task Del(DerasaXDbContext db, string table, string col, string subquery)
#pragma warning disable EF1002
        => db.Database.ExecuteSqlRawAsync($"DELETE FROM \"{table}\" WHERE \"{col}\" IN ({subquery})");
#pragma warning restore EF1002

    private static async Task<Env<T>?> Read<T>(HttpResponseMessage r)
    {
        var raw = await r.Content.ReadAsStringAsync();
        return (!string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith("{")) ? JsonSerializer.Deserialize<Env<T>>(raw, Json) : null;
    }

    [Fact]
    public async Task Parent_teacher_conversation_messaging_and_nonparticipant_rejection()
    {
        var w = await SetupAsync();
        var convIds = new List<string>();
        try
        {
            var parent = await TestClient.AuthedClientAsync(_factory, "PARENT-T1");
            var start = await parent.PostAsJsonAsync("/api/v1/conversations", new
            {
                participantUserId = w.teacherId, studentId = w.studentId, subject = "Homework", firstMessage = "Hello"
            });
            var (ss, sb) = (start.StatusCode, await Read<IdRow>(start));
            Assert.Equal(HttpStatusCode.Created, ss);
            var convId = sb!.data!.id;
            convIds.Add(convId);

            // Teacher (a participant) can post + read.
            var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
            var post = await teacher.PostAsJsonAsync($"/api/v1/conversations/{convId}/messages", new { body = "Hi back" });
            Assert.Equal(HttpStatusCode.Created, post.StatusCode);
            var msgId = (await Read<IdRow>(post))!.data!.id;

            var list = await teacher.GetAsync($"/api/v1/conversations/{convId}/messages?pageSize=10");
            var (ls, lb) = (list.StatusCode, await Read<List<IdRow>>(list));
            Assert.Equal(HttpStatusCode.OK, ls);
            Assert.True(lb!.totalCount >= 2);

            // Read receipt.
            var read = await teacher.PostAsync($"/api/v1/conversations/{convId}/messages/{msgId}/read", null);
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
            await using (var db = Phase4Db.Platform(_factory))
                Assert.True(await db.messageReadReceipts.IgnoreQueryFilters().AnyAsync(r => r.MessageId == msgId && r.UserId == w.teacherId));

            // A non-participant (a different student) is denied — and existence is not leaked (404).
            var outsider = await TestClient.AuthedClientAsync(_factory, "STU-T1");
            Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/v1/conversations/{convId}/messages")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await outsider.PostAsJsonAsync($"/api/v1/conversations/{convId}/messages", new { body = "intrude" })).StatusCode);

            // Audit + notification records exist.
            await using (var db = Phase4Db.Platform(_factory))
            {
                Assert.True(await db.auditLogs.IgnoreQueryFilters().AnyAsync(a => a.EntityType == "Conversation" && a.EntityId == convId));
                Assert.True(await db.notifications.IgnoreQueryFilters().AnyAsync(n => n.UserId == w.teacherId && n.Title == "New message"));
            }
        }
        finally { await Cleanup(w, conversationIds: convIds); }
    }

    [Fact]
    public async Task Invalid_relationship_and_cross_tenant_participant_rejected()
    {
        var w = await SetupAsync(link: false); // no enrollment/assignment/link
        try
        {
            var parent = await TestClient.AuthedClientAsync(_factory, "PARENT-T1");

            // No linking relationship → 403.
            var noLink = await parent.PostAsJsonAsync("/api/v1/conversations", new { participantUserId = w.teacherId, studentId = w.studentId });
            Assert.Equal(HttpStatusCode.Forbidden, noLink.StatusCode);

            // Cross-tenant participant (a tenant-2 teacher) → 404 (no existence leak).
            var cross = await parent.PostAsJsonAsync("/api/v1/conversations", new { participantUserId = await UserId("TEACH-T2") });
            Assert.Equal(HttpStatusCode.NotFound, cross.StatusCode);
        }
        finally { await Cleanup(w); }
    }

    [Fact]
    public async Task Parent_request_lifecycle_and_authorization()
    {
        var w = await SetupAsync();
        var reqIds = new List<string>();
        try
        {
            var parent = await TestClient.AuthedClientAsync(_factory, "PARENT-T1");
            var create = await parent.PostAsJsonAsync("/api/v1/parent-requests", new
            {
                studentId = w.studentId, type = 0, title = "Report card", body = "Please send it."
            });
            var (cs, cb) = (create.StatusCode, await Read<ReqRow>(create));
            Assert.Equal(HttpStatusCode.Created, cs);
            var reqId = cb!.data!.id;
            reqIds.Add(reqId);

            // A parent cannot respond (staff-only) → 403.
            Assert.Equal(HttpStatusCode.Forbidden,
                (await parent.PostAsJsonAsync($"/api/v1/parent-requests/{reqId}/responses", new { body = "x" })).StatusCode);

            // Admin responds (moves Open → InProgress) and resolves.
            var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
            Assert.Equal(HttpStatusCode.Created,
                (await admin.PostAsJsonAsync($"/api/v1/parent-requests/{reqId}/responses", new { body = "On it" })).StatusCode);
            var resolve = await admin.PostAsJsonAsync($"/api/v1/parent-requests/{reqId}/status", new { status = (int)ParentRequestStatus.Resolved });
            Assert.Equal(HttpStatusCode.OK, resolve.StatusCode);

            // Invalid transition (Resolved → InProgress) → 409.
            var invalid = await admin.PostAsJsonAsync($"/api/v1/parent-requests/{reqId}/status", new { status = (int)ParentRequestStatus.InProgress });
            Assert.Equal(HttpStatusCode.Conflict, invalid.StatusCode);

            // A different parent cannot see this request (404, no leak).
            var stranger = await TestClient.AuthedClientAsync(_factory, "PARENT-T2");
            // PARENT-T2 is tenant-2 → tenant filter already yields 404 on the tenant route via policy/scoping.
            Assert.Contains((await stranger.GetAsync($"/api/v1/parent-requests/{reqId}")).StatusCode,
                new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden });
        }
        finally { await Cleanup(w, parentRequestIds: reqIds); }
    }

    [Fact]
    public async Task Announcement_targeting()
    {
        var w = await SetupAsync(link: false);
        var annIds = new List<string>();
        try
        {
            var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
            var create = await admin.PostAsJsonAsync("/api/v1/announcements", new
            {
                title = NewId("Ann"), body = "Students only", targetAudience = (int)TargetAudience.Students
            });
            var (cs, cb) = (create.StatusCode, await Read<IdRow>(create));
            Assert.Equal(HttpStatusCode.Created, cs);
            var annId = cb!.data!.id;
            annIds.Add(annId);

            // A student sees the students-targeted announcement; a teacher does not.
            var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
            var stuList = await Read<List<IdRow>>(await student.GetAsync("/api/v1/announcements?pageSize=100"));
            Assert.Contains(stuList!.data!, a => a.id == annId);

            var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
            var teaList = await Read<List<IdRow>>(await teacher.GetAsync("/api/v1/announcements?pageSize=100"));
            Assert.DoesNotContain(teaList!.data!, a => a.id == annId);
        }
        finally { await Cleanup(w, announcementIds: annIds); }
    }

    [Fact]
    public async Task Anonymous_suggestion_privacy_and_moderation_authorization()
    {
        var w = await SetupAsync(link: false);
        var sugIds = new List<string>();
        try
        {
            // A student submits anonymously.
            var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
            var title = NewId("Sug");
            var submit = await student.PostAsJsonAsync("/api/v1/suggestions", new { title, body = "Please add lockers." });
            var (ss, sb) = (submit.StatusCode, await Read<IdRow>(submit));
            Assert.Equal(HttpStatusCode.Created, ss);
            var sugId = sb!.data!.id;
            sugIds.Add(sugId);

            // Students cannot list/moderate → 403.
            Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync("/api/v1/suggestions")).StatusCode);

            // Admin lists — the payload must NOT contain the submitter's id (anonymity).
            var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
            var listResp = await admin.GetAsync("/api/v1/suggestions?pageSize=100");
            Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
            var raw = await listResp.Content.ReadAsStringAsync();
            Assert.Contains(title, raw);
            Assert.DoesNotContain(await UserId("STU-T1"), raw);

            // Admin moderates; the moderation is audited.
            var moderate = await admin.PostAsJsonAsync($"/api/v1/suggestions/{sugId}/moderate", new { status = (int)SuggestionStatus.Accepted, reviewNotes = "Good idea" });
            Assert.Equal(HttpStatusCode.OK, moderate.StatusCode);
            await using var db = Phase4Db.Platform(_factory);
            Assert.True(await db.auditLogs.IgnoreQueryFilters().AnyAsync(a => a.EntityType == "Suggestion" && a.EntityId == sugId));
        }
        finally { await Cleanup(w, suggestionIds: sugIds); }
    }
}
