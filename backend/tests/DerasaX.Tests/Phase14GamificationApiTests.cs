using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DerasaX.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 14 — ledger-based gamification through the real HTTP pipeline: idempotent + bounded +
/// authorized manual point awards, tenant-scoped points leaderboard, office-hour attendance and
/// competition close awarding points to the ledger WITHOUT double-counting, and admin-only
/// gamification rule management. Cross-tenant access is rejected.
/// </summary>
public class Phase14GamificationApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public Phase14GamificationApiTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private sealed record Env<T>(bool success, int statusCode, T? data, int totalCount);
    private sealed record IdRow(string id);
    private sealed record EntryRow(string id, string studentId);
    private sealed record Summary(string studentId, int totalPoints, int transactionCount, int badgeCount);
    private sealed record LbRow(string studentId, int totalPoints, int rank);
    private sealed record RuleRow(string id, string code, int points, bool enabled);

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

    private async Task<int> TotalPoints(HttpClient client, string studentId)
    {
        var s = await Read<Summary>(await client.GetAsync($"/api/v1/students/{studentId}/points"));
        return s!.data!.totalPoints;
    }

    [Fact]
    public async Task Manual_award_is_idempotent_bounded_authorized_and_tenant_scoped()
    {
        var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var stuId = await UserId("STU-T1");
        var key = NewId("ph14key");
        try
        {
            var before = await TotalPoints(admin, stuId);

            // Authorized manual award.
            var award = await admin.PostAsJsonAsync($"/api/v1/students/{stuId}/points",
                new { points = 50, reason = "Phase 14 manual award", idempotencyKey = key });
            Assert.Equal(HttpStatusCode.Created, award.StatusCode);
            Assert.Equal(before + 50, await TotalPoints(admin, stuId));

            // Idempotent retry with the SAME key → 200 and NO double-count.
            var dup = await admin.PostAsJsonAsync($"/api/v1/students/{stuId}/points",
                new { points = 50, reason = "Phase 14 manual award", idempotencyKey = key });
            Assert.Equal(HttpStatusCode.OK, dup.StatusCode);
            Assert.Equal(before + 50, await TotalPoints(admin, stuId));

            // Bounds: zero and out-of-range are rejected.
            Assert.Equal(HttpStatusCode.BadRequest,
                (await admin.PostAsJsonAsync($"/api/v1/students/{stuId}/points", new { points = 0, reason = "x" })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,
                (await admin.PostAsJsonAsync($"/api/v1/students/{stuId}/points", new { points = 999999, reason = "x" })).StatusCode);

            // A student cannot award points (cannot award themselves) → 403.
            Assert.Equal(HttpStatusCode.Forbidden,
                (await student.PostAsJsonAsync($"/api/v1/students/{stuId}/points", new { points = 10, reason = "self" })).StatusCode);

            // Cross-tenant: a tenant-2 admin cannot award to / read a tenant-1 student → 404.
            var admin2 = await TestClient.AuthedClientAsync(_factory, "ADMIN-T2");
            Assert.Equal(HttpStatusCode.NotFound,
                (await admin2.PostAsJsonAsync($"/api/v1/students/{stuId}/points", new { points = 10, reason = "x" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await admin2.GetAsync($"/api/v1/students/{stuId}/points")).StatusCode);

            // Ledger lists the transaction.
            var ledger = await Read<List<JsonElement>>(await admin.GetAsync($"/api/v1/students/{stuId}/points/ledger?pageSize=100"));
            Assert.True((ledger!.totalCount) >= 1);
        }
        finally { await CleanupStudentPoints(stuId); }
    }

    [Fact]
    public async Task Points_leaderboard_is_tenant_scoped()
    {
        var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var stuId = await UserId("STU-T1");
        try
        {
            await admin.PostAsJsonAsync($"/api/v1/students/{stuId}/points",
                new { points = 777, reason = "Phase 14 leaderboard seed", idempotencyKey = NewId("lb") });

            var board = await Read<List<LbRow>>(await admin.GetAsync("/api/v1/gamification/leaderboard?pageSize=100"));
            Assert.Contains(board!.data!, r => r.studentId == stuId);

            // The tenant-2 leaderboard must NOT contain a tenant-1 student.
            var admin2 = await TestClient.AuthedClientAsync(_factory, "ADMIN-T2");
            var board2 = await Read<List<LbRow>>(await admin2.GetAsync("/api/v1/gamification/leaderboard?pageSize=100"));
            Assert.DoesNotContain(board2!.data!, r => r.studentId == stuId);
        }
        finally { await CleanupStudentPoints(stuId); }
    }

    [Fact]
    public async Task Office_hour_attendance_awards_points_idempotently_and_is_staff_only()
    {
        var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
        var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var stuId = await UserId("STU-T1");
        string sessionId = "";
        try
        {
            var create = await teacher.PostAsJsonAsync("/api/v1/office-hours", new
            {
                title = "PH14 Help", startsAt = new DateTime(2035, 2, 1, 10, 0, 0, DateTimeKind.Utc),
                endsAt = new DateTime(2035, 2, 1, 11, 0, 0, DateTimeKind.Utc), capacity = 3
            });
            sessionId = (await Read<IdRow>(create))!.data!.id;
            var book = await student.PostAsJsonAsync($"/api/v1/office-hours/{sessionId}/bookings", new { notes = "ph14" });
            var bookingId = (await Read<IdRow>(book))!.data!.id;

            var before = await TotalPoints(teacher, stuId);

            // A student cannot mark attendance → 403.
            Assert.Equal(HttpStatusCode.Forbidden,
                (await student.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/attendance", new { status = (int)OfficeHourBookingStatus.Attended })).StatusCode);

            // The owning teacher marks attendance → points awarded once.
            var mark = await teacher.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/attendance", new { status = (int)OfficeHourBookingStatus.Attended });
            Assert.Equal(HttpStatusCode.OK, mark.StatusCode);
            var afterFirst = await TotalPoints(teacher, stuId);
            Assert.Equal(before + GamificationDefaultPoints.OfficeHourAttended, afterFirst);

            // Marking attendance again must NOT double-award.
            Assert.Equal(HttpStatusCode.OK,
                (await teacher.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/attendance", new { status = (int)OfficeHourBookingStatus.Attended })).StatusCode);
            Assert.Equal(afterFirst, await TotalPoints(teacher, stuId));
        }
        finally
        {
            await CleanupStudentPoints(stuId);
            await using var db = Phase4Db.Platform(_factory);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"officeHourBookings\" WHERE \"OfficeHourSessionId\" = {0}", sessionId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"officeHourSessions\" WHERE \"Id\" = {0}", sessionId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"notifications\" WHERE \"UserId\" = {0} AND \"Title\" IN ('New office-hour booking','Points awarded')", stuId);
        }
    }

    [Fact]
    public async Task Competition_close_publishes_results_and_rewards_without_double_award()
    {
        var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
        var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var stuId = await UserId("STU-T1");
        string compId = "";
        try
        {
            var create = await teacher.PostAsJsonAsync("/api/v1/competitions", new
            {
                title = NewId("PH14Comp"), startsAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                endsAt = new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
            compId = (await Read<IdRow>(create))!.data!.id;
            await teacher.PostAsync($"/api/v1/competitions/{compId}/publish", null);
            var enter = await student.PostAsync($"/api/v1/competitions/{compId}/entries", null);
            var entryId = (await Read<EntryRow>(enter))!.data!.id;
            await teacher.PostAsJsonAsync($"/api/v1/competitions/{compId}/entries/{entryId}/score", new { score = 42 });

            var before = await TotalPoints(teacher, stuId);

            // Close + publish results → top-rank + participation rewards land in the ledger once.
            Assert.Equal(HttpStatusCode.OK, (await teacher.PostAsync($"/api/v1/competitions/{compId}/close", null)).StatusCode);
            var expected = before + GamificationDefaultPoints.CompetitionParticipation + GamificationDefaultPoints.CompetitionTopRank;
            Assert.Equal(expected, await TotalPoints(teacher, stuId));

            // Re-closing is rejected (no second award).
            Assert.Equal(HttpStatusCode.Conflict, (await teacher.PostAsync($"/api/v1/competitions/{compId}/close", null)).StatusCode);
            Assert.Equal(expected, await TotalPoints(teacher, stuId));

            // After close, the student may view the leaderboard (result-visibility timing).
            Assert.Equal(HttpStatusCode.OK, (await student.GetAsync($"/api/v1/competitions/{compId}/leaderboard")).StatusCode);
        }
        finally
        {
            await CleanupStudentPoints(stuId);
            await using var db = Phase4Db.Platform(_factory);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"competitionScores\" WHERE \"CompetitionEntryId\" IN (SELECT \"Id\" FROM \"competitionEntries\" WHERE \"CompetitionId\" = {0})", compId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"competitionEntries\" WHERE \"CompetitionId\" = {0}", compId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"competitions\" WHERE \"Id\" = {0}", compId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"notifications\" WHERE \"UserId\" = {0} AND \"Title\" = 'Competition results published'", stuId);
        }
    }

    [Fact]
    public async Task Gamification_rules_are_admin_managed_and_teacher_readable()
    {
        var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
        var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var code = NewId("PH14R")[..18];
        try
        {
            // Admin upserts a rule.
            var up = await admin.PutAsJsonAsync("/api/v1/gamification/rules",
                new { code, name = "Office hour reward", trigger = (int)GamificationTrigger.OfficeHourAttended, points = 15, enabled = true });
            Assert.Equal(HttpStatusCode.OK, up.StatusCode);

            // A teacher may read the rules.
            var rules = await Read<List<RuleRow>>(await teacher.GetAsync("/api/v1/gamification/rules"));
            Assert.Contains(rules!.data!, r => r.code == code && r.points == 15);

            // A student may neither read nor manage rules.
            Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync("/api/v1/gamification/rules")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await student.PutAsJsonAsync("/api/v1/gamification/rules", new { code, name = "x", trigger = 0, points = 1, enabled = true })).StatusCode);

            // A teacher cannot manage (upsert) rules → 403.
            Assert.Equal(HttpStatusCode.Forbidden,
                (await teacher.PutAsJsonAsync("/api/v1/gamification/rules", new { code, name = "x", trigger = 0, points = 1, enabled = true })).StatusCode);
        }
        finally
        {
            await using var db = Phase4Db.Platform(_factory);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"gamificationRules\" WHERE \"Code\" = {0}", code);
        }
    }

    private async Task CleanupStudentPoints(string studentId)
    {
        await using var db = Phase4Db.Platform(_factory);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"studentPointTransactions\" WHERE \"StudentId\" = {0}", studentId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"notifications\" WHERE \"UserId\" = {0} AND \"Title\" = 'Points awarded'", studentId);
    }

    // Mirror of the server-side GamificationDefaults so the assertions read against the same constants.
    private static class GamificationDefaultPoints
    {
        public const int OfficeHourAttended = 10;
        public const int CompetitionTopRank = 50;
        public const int CompetitionParticipation = 10;
    }
}
