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
/// Phase 5 §13 (Increment 6) — engagement workflows through the real HTTP pipeline: community
/// membership/posting/moderation, competition entry (duplicate prevention) and staff-only
/// scoring with result-visibility timing, idempotent badge awards with authorization, and
/// office-hour capacity/duplicate/cross-tenant booking rules.
/// </summary>
public class EngagementApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public EngagementApiTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private sealed record Env<T>(bool success, int statusCode, T? data, int totalCount);
    private sealed record IdRow(string id);
    private sealed record EntryRow(string id, string studentId);
    private sealed record BadgeRow(string id, string badgeId);
    private sealed record LbRow(string studentId, decimal score, int rank);
    private sealed record CompRow(string id, int status, bool hasEntered, bool canEnter, bool canSubmit, bool canViewLeaderboard);

    private static string NewId(string p) => $"{p}-{Guid.NewGuid():N}";

    private async Task<string> UserId(string loginCode)
    {
        await using var db = Phase4Db.Platform(_factory);
        return (await db.applicationUsers.IgnoreQueryFilters().FirstAsync(u => u.LoginCode == loginCode)).Id;
    }

    private static async Task<Env<T>?> Read<T>(HttpResponseMessage r)
    {
        var raw = await r.Content.ReadAsStringAsync();
        return (!string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith("{")) ? JsonSerializer.Deserialize<Env<T>>(raw, Json) : null;
    }

    private static async Task Del(DerasaXDbContext db, string table, string col, string id)
#pragma warning disable EF1002
        => await db.Database.ExecuteSqlRawAsync($"DELETE FROM \"{table}\" WHERE \"{col}\" = {{0}}", id);
#pragma warning restore EF1002

    [Fact]
    public async Task Community_membership_posting_and_moderation()
    {
        var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
        var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        string communityId = "";
        try
        {
            var create = await teacher.PostAsJsonAsync("/api/v1/communities", new { name = NewId("Comm"), visibility = (int)CommunityVisibility.TenantOnly });
            var (cs, cb) = (create.StatusCode, await Read<IdRow>(create));
            Assert.Equal(HttpStatusCode.Created, cs);
            communityId = cb!.data!.id;

            // A non-member cannot post → 403.
            Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsJsonAsync($"/api/v1/communities/{communityId}/posts", new { content = "hi" })).StatusCode);

            // Join, then post.
            Assert.Equal(HttpStatusCode.OK, (await student.PostAsync($"/api/v1/communities/{communityId}/join", null)).StatusCode);
            var post = await student.PostAsJsonAsync($"/api/v1/communities/{communityId}/posts", new { content = "Hello community" });
            Assert.Equal(HttpStatusCode.Created, post.StatusCode);
            var postId = (await Read<IdRow>(post))!.data!.id;

            // Report, then owner moderates (removes the post).
            Assert.Equal(HttpStatusCode.Created, (await student.PostAsJsonAsync($"/api/v1/posts/{postId}/reports", new { reason = "spam" })).StatusCode);
            var moderate = await teacher.PostAsJsonAsync($"/api/v1/posts/{postId}/moderate", new { status = (int)ReportStatus.ActionTaken, removePost = true });
            Assert.Equal(HttpStatusCode.OK, moderate.StatusCode);

            // Cross-tenant read → 404.
            var admin2 = await TestClient.AuthedClientAsync(_factory, "ADMIN-T2");
            Assert.Equal(HttpStatusCode.NotFound, (await admin2.GetAsync($"/api/v1/communities/{communityId}")).StatusCode);
        }
        finally
        {
            await using var db = Phase4Db.Platform(_factory);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"postReports\" WHERE \"PostId\" IN (SELECT \"Id\" FROM \"posts\" WHERE \"CommunityId\" = {0})", communityId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"postComments\" WHERE \"PostId\" IN (SELECT \"Id\" FROM \"posts\" WHERE \"CommunityId\" = {0})", communityId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"posts\" WHERE \"CommunityId\" = {0}", communityId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"communityMemberships\" WHERE \"CommunityId\" = {0}", communityId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"communities\" WHERE \"Id\" = {0}", communityId);
        }
    }

    [Fact]
    public async Task Competition_entry_duplicate_scoring_and_visibility()
    {
        var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
        var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        string compId = "";
        try
        {
            var create = await teacher.PostAsJsonAsync("/api/v1/competitions", new
            {
                title = NewId("Comp"), startsAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), endsAt = new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
            compId = (await Read<IdRow>(create))!.data!.id;
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            await teacher.PostAsync($"/api/v1/competitions/{compId}/publish", null);

            // Student enters; duplicate entry rejected.
            var enter = await student.PostAsync($"/api/v1/competitions/{compId}/entries", null);
            var (es, eb) = (enter.StatusCode, await Read<EntryRow>(enter));
            Assert.Equal(HttpStatusCode.Created, es);
            Assert.Equal(HttpStatusCode.Conflict, (await student.PostAsync($"/api/v1/competitions/{compId}/entries", null)).StatusCode);

            // A student cannot record a score (score integrity) → 403.
            Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsJsonAsync($"/api/v1/competitions/{compId}/entries/{eb!.data!.id}/score", new { score = 99 })).StatusCode);

            // Teacher records the score; leaderboard reflects it.
            Assert.Equal(HttpStatusCode.OK, (await teacher.PostAsJsonAsync($"/api/v1/competitions/{compId}/entries/{eb.data.id}/score", new { score = 42 })).StatusCode);
            var lb = await Read<List<LbRow>>(await teacher.GetAsync($"/api/v1/competitions/{compId}/leaderboard"));
            Assert.Contains(lb!.data!, r => r.studentId == eb.data.studentId && r.score == 42m);

            // Visibility timing: a student cannot see the leaderboard while the competition is only Published.
            Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync($"/api/v1/competitions/{compId}/leaderboard")).StatusCode);
        }
        finally
        {
            await using var db = Phase4Db.Platform(_factory);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"competitionScores\" WHERE \"CompetitionEntryId\" IN (SELECT \"Id\" FROM \"competitionEntries\" WHERE \"CompetitionId\" = {0})", compId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"competitionEntries\" WHERE \"CompetitionId\" = {0}", compId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"competitions\" WHERE \"Id\" = {0}", compId);
        }
    }

    [Fact]
    public async Task Competition_student_visibility_and_flags()
    {
        var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
        var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        string compId = "";
        try
        {
            var create = await teacher.PostAsJsonAsync("/api/v1/competitions", new
            {
                title = NewId("Comp"), startsAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), endsAt = new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
            compId = (await Read<IdRow>(create))!.data!.id;

            // Draft is NOT visible to a student (server-side filter) and a direct GET 404s.
            var draftList = await Read<List<CompRow>>(await student.GetAsync("/api/v1/competitions?pageSize=100"));
            Assert.DoesNotContain(draftList!.data!, c => c.id == compId);
            Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync($"/api/v1/competitions/{compId}")).StatusCode);

            // Publish → visible with canEnter, not yet entered, leaderboard not released.
            await teacher.PostAsync($"/api/v1/competitions/{compId}/publish", null);
            var pubDetail = (await Read<CompRow>(await student.GetAsync($"/api/v1/competitions/{compId}")))!.data!;
            Assert.True(pubDetail.canEnter);
            Assert.False(pubDetail.hasEntered);
            Assert.False(pubDetail.canSubmit);
            Assert.False(pubDetail.canViewLeaderboard);

            // After entry → hasEntered, canSubmit, no longer canEnter.
            await student.PostAsync($"/api/v1/competitions/{compId}/entries", null);
            var enteredDetail = (await Read<CompRow>(await student.GetAsync($"/api/v1/competitions/{compId}")))!.data!;
            Assert.True(enteredDetail.hasEntered);
            Assert.True(enteredDetail.canSubmit);
            Assert.False(enteredDetail.canEnter);
            Assert.False(enteredDetail.canViewLeaderboard);

            // After close → results released to the student; no further entry/submission.
            await teacher.PostAsync($"/api/v1/competitions/{compId}/close", null);
            var closedDetail = (await Read<CompRow>(await student.GetAsync($"/api/v1/competitions/{compId}")))!.data!;
            Assert.True(closedDetail.canViewLeaderboard);
            Assert.False(closedDetail.canEnter);
            Assert.False(closedDetail.canSubmit);
            Assert.Equal(HttpStatusCode.OK, (await student.GetAsync($"/api/v1/competitions/{compId}/leaderboard")).StatusCode);
        }
        finally
        {
            await using var db = Phase4Db.Platform(_factory);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"competitionScores\" WHERE \"CompetitionEntryId\" IN (SELECT \"Id\" FROM \"competitionEntries\" WHERE \"CompetitionId\" = {0})", compId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"competitionEntries\" WHERE \"CompetitionId\" = {0}", compId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"competitions\" WHERE \"Id\" = {0}", compId);
        }
    }

    [Fact]
    public async Task Badge_award_idempotency_and_authorization()
    {
        var badgeId = NewId("badge");
        var stuId = await UserId("STU-T1");
        await using (var db = Phase4Db.Platform(_factory))
        {
            db.badges.Add(new Badge { Id = badgeId, Code = NewId("B")[..12], Name = "Star", Type = BadgeType.Achievement });
            await db.SaveChangesAsync();
        }
        try
        {
            var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
            var award = await admin.PostAsJsonAsync($"/api/v1/students/{stuId}/badges", new { badgeId, reason = "Great work" });
            Assert.Equal(HttpStatusCode.Created, award.StatusCode);

            // Idempotency: a second award of the same badge → 409.
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/v1/students/{stuId}/badges", new { badgeId })).StatusCode);

            // A student cannot award badges → 403.
            var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
            Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsJsonAsync($"/api/v1/students/{stuId}/badges", new { badgeId })).StatusCode);

            // The student sees the awarded badge.
            var list = await Read<List<BadgeRow>>(await student.GetAsync($"/api/v1/students/{stuId}/badges"));
            Assert.Contains(list!.data!, b => b.badgeId == badgeId);
        }
        finally
        {
            await using var db = Phase4Db.Platform(_factory);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"studentBadges\" WHERE \"BadgeId\" = {0}", badgeId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"notifications\" WHERE \"UserId\" = {0} AND \"Title\" = 'Badge awarded'", stuId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"badges\" WHERE \"Id\" = {0}", badgeId);
        }
    }

    [Fact]
    public async Task Office_hour_capacity_duplicate_and_cross_tenant_booking()
    {
        var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
        var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var other = await TestUsers.CreateLockoutStudentAsync(_factory);
        var otherClient = await TestClient.AuthedClientAsync(_factory, other.LoginCode);
        string sessionId = "";
        try
        {
            var create = await teacher.PostAsJsonAsync("/api/v1/office-hours", new
            {
                title = "Help", startsAt = new DateTime(2035, 1, 1, 10, 0, 0, DateTimeKind.Utc), endsAt = new DateTime(2035, 1, 1, 11, 0, 0, DateTimeKind.Utc), capacity = 1
            });
            sessionId = (await Read<IdRow>(create))!.data!.id;
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);

            // First booking succeeds; duplicate by the same student → 409.
            var book = await student.PostAsJsonAsync($"/api/v1/office-hours/{sessionId}/bookings", new { notes = "thanks" });
            Assert.Equal(HttpStatusCode.Created, book.StatusCode);
            var bookingId = (await Read<IdRow>(book))!.data!.id;
            Assert.Equal(HttpStatusCode.Conflict, (await student.PostAsJsonAsync($"/api/v1/office-hours/{sessionId}/bookings", new { })).StatusCode);

            // Capacity is 1 → a second student is rejected.
            Assert.Equal(HttpStatusCode.Conflict, (await otherClient.PostAsJsonAsync($"/api/v1/office-hours/{sessionId}/bookings", new { })).StatusCode);

            // Cancelling frees the slot; the other student can then book.
            Assert.Equal(HttpStatusCode.OK, (await student.PostAsync($"/api/v1/bookings/{bookingId}/cancel", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await otherClient.PostAsJsonAsync($"/api/v1/office-hours/{sessionId}/bookings", new { })).StatusCode);

            // Cross-tenant student cannot see/book the session → 404.
            var t2 = await TestClient.AuthedClientAsync(_factory, "STU-T2");
            Assert.Equal(HttpStatusCode.NotFound, (await t2.PostAsJsonAsync($"/api/v1/office-hours/{sessionId}/bookings", new { })).StatusCode);
        }
        finally
        {
            await using var db = Phase4Db.Platform(_factory);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"officeHourBookings\" WHERE \"OfficeHourSessionId\" = {0}", sessionId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"officeHourSessions\" WHERE \"Id\" = {0}", sessionId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"notifications\" WHERE \"Title\" IN ('New office-hour booking','Office hours cancelled')");
        }
    }
}
