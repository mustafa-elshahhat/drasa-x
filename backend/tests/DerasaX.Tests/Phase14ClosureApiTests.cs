using System;
using System.Collections.Generic;
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
/// Phase 14 (closure patch) — the two new backend-connected behaviours added to close the documented
/// Phase 14 gaps: durable competition submissions (student submits/resubmits work that an entry exists
/// for; staff-only listing) and the community grade-eligibility join gate (a grade-restricted community
/// only admits a student whose grade matches). Both are tenant-safe and role-authorized.
/// </summary>
public class Phase14ClosureApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public Phase14ClosureApiTests(IntegrationFactory factory) => _factory = factory;

    // Stable tenant-1 seed grades: STU-T1 is in G7-ID; G8-ID is a different tenant-1 grade.
    private const string StudentGradeId = "G7-ID";
    private const string OtherGradeId = "G8-ID";

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private sealed record Env<T>(bool success, int statusCode, T? data, int totalCount);
    private sealed record IdRow(string id);
    private sealed record SubmissionRow(string id, string competitionId, string studentId, string content);

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

    [Fact]
    public async Task Competition_submission_requires_entry_is_durable_and_staff_listable()
    {
        var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
        var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var stuId = await UserId("STU-T1");
        string compId = "";
        try
        {
            var create = await teacher.PostAsJsonAsync("/api/v1/competitions", new
            {
                title = NewId("PH14Sub"), startsAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                endsAt = new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
            compId = (await Read<IdRow>(create))!.data!.id;
            await teacher.PostAsync($"/api/v1/competitions/{compId}/publish", null);

            // A student must enter before submitting work → 409.
            Assert.Equal(HttpStatusCode.Conflict,
                (await student.PostAsJsonAsync($"/api/v1/competitions/{compId}/submissions", new { content = "early" })).StatusCode);

            // Enter, then submit → 201.
            await student.PostAsync($"/api/v1/competitions/{compId}/entries", null);
            var submit = await student.PostAsJsonAsync($"/api/v1/competitions/{compId}/submissions", new { content = "My first answer" });
            Assert.Equal(HttpStatusCode.Created, submit.StatusCode);

            // Durable resubmission updates the same submission in place → 200.
            var resubmit = await student.PostAsJsonAsync($"/api/v1/competitions/{compId}/submissions", new { content = "My revised answer" });
            Assert.Equal(HttpStatusCode.OK, resubmit.StatusCode);

            // The student reads their own submission (latest content).
            var mine = await Read<SubmissionRow>(await student.GetAsync($"/api/v1/competitions/{compId}/submissions/me"));
            Assert.Equal("My revised answer", mine!.data!.content);

            // Staff list all submissions (to judge) — exactly one, with the revised content.
            var listResp = await teacher.GetAsync($"/api/v1/competitions/{compId}/submissions");
            Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
            var list = await Read<List<SubmissionRow>>(listResp);
            Assert.Contains(list!.data!, s => s.studentId == stuId && s.content == "My revised answer");

            // A student cannot list every submission (staff-only judging surface) → 403.
            Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync($"/api/v1/competitions/{compId}/submissions")).StatusCode);

            // Cross-tenant staff cannot read submissions for a tenant-1 competition → 404.
            var teacher2 = await TestClient.AuthedClientAsync(_factory, "TEACH-T2");
            Assert.Equal(HttpStatusCode.NotFound, (await teacher2.GetAsync($"/api/v1/competitions/{compId}/submissions")).StatusCode);
        }
        finally
        {
            await using var db = Phase4Db.Platform(_factory);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"competitionSubmissions\" WHERE \"CompetitionId\" = {0}", compId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"competitionEntries\" WHERE \"CompetitionId\" = {0}", compId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"competitions\" WHERE \"Id\" = {0}", compId);
        }
    }

    [Fact]
    public async Task Community_grade_eligibility_gate_admits_matching_grade_and_rejects_others()
    {
        var teacher = await TestClient.AuthedClientAsync(_factory, "TEACH-T1");
        var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        string eligibleId = "", ineligibleId = "";
        try
        {
            // A community gated to the student's own grade → the student may self-join.
            var okCreate = await teacher.PostAsJsonAsync("/api/v1/communities",
                new { name = NewId("GradeOK"), visibility = (int)CommunityVisibility.TenantOnly, eligibleGradeId = StudentGradeId });
            Assert.Equal(HttpStatusCode.Created, okCreate.StatusCode);
            eligibleId = (await Read<IdRow>(okCreate))!.data!.id;
            Assert.Equal(HttpStatusCode.OK, (await student.PostAsync($"/api/v1/communities/{eligibleId}/join", null)).StatusCode);

            // A community gated to a DIFFERENT grade → the student is rejected with 403.
            var noCreate = await teacher.PostAsJsonAsync("/api/v1/communities",
                new { name = NewId("GradeNo"), visibility = (int)CommunityVisibility.TenantOnly, eligibleGradeId = OtherGradeId });
            ineligibleId = (await Read<IdRow>(noCreate))!.data!.id;
            Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsync($"/api/v1/communities/{ineligibleId}/join", null)).StatusCode);

            // Creating a community gated to a non-existent grade → 400 (cannot gate on a foreign grade).
            Assert.Equal(HttpStatusCode.BadRequest,
                (await teacher.PostAsJsonAsync("/api/v1/communities",
                    new { name = NewId("GradeBad"), visibility = (int)CommunityVisibility.TenantOnly, eligibleGradeId = "no-such-grade" })).StatusCode);
        }
        finally
        {
            await using var db = Phase4Db.Platform(_factory);
            foreach (var id in new[] { eligibleId, ineligibleId })
            {
                if (string.IsNullOrEmpty(id)) continue;
                await db.Database.ExecuteSqlRawAsync("DELETE FROM \"communityMemberships\" WHERE \"CommunityId\" = {0}", id);
                await db.Database.ExecuteSqlRawAsync("DELETE FROM \"communities\" WHERE \"Id\" = {0}", id);
            }
        }
    }
}
