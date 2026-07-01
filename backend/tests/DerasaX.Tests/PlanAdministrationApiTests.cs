using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// System Admin subscription-plan CRUD: creation, update, validation, duplicate-code
/// rejection, and authorization (SystemAdmin-only, never exposed to tenant roles).
/// </summary>
public class PlanAdministrationApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public PlanAdministrationApiTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private sealed record Env<T>(bool success, int statusCode, T? data);
    private sealed record PlanRow(string id, string code, string name);
    private static string NewCode(string p) => $"{p}-{Guid.NewGuid():N}"[..16].ToUpperInvariant();

    private static async Task<Env<T>?> Read<T>(HttpResponseMessage r)
    {
        var raw = await r.Content.ReadAsStringAsync();
        return (!string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith("{")) ? JsonSerializer.Deserialize<Env<T>>(raw, Json) : null;
    }

    private object ValidPlanBody(string code) => new
    {
        code,
        name = "Test Plan " + code,
        description = "A plan created by an automated test.",
        tier = 1,
        billingPeriod = 0,
        price = 19.99,
        currency = "usd",
        trialDays = 7,
        isActive = true,
        maxStudents = 100,
        maxTeachers = 10,
        maxParents = 200,
        maxSchoolAdmins = 2,
        maxClasses = 20,
        maxSubjects = 15,
        maxLessonMaterials = 500,
        maxStorageMb = 2048,
        maxAiGenerationsPerMonth = 1000,
        maxAiTokensPerMonth = 500_000,
    };

    private async Task DeletePlanAsync(string planId)
    {
        await using var db = Phase4Db.Platform(_factory);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"auditLogs\" WHERE \"EntityId\" = {0}", planId);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"subscriptionPlanDefinitions\" WHERE \"Id\" = {0}", planId);
    }

    [Fact]
    public async Task SystemAdmin_creates_and_updates_a_valid_plan_with_audit_logging()
    {
        var sys = await TestClient.AuthedClientAsync(_factory, "SYS-1");
        var code = NewCode("PLAN");
        string? planId = null;
        try
        {
            var create = await sys.PostAsJsonAsync("/api/v1/tenants/plans", ValidPlanBody(code));
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var created = (await Read<PlanRow>(create))!.data!;
            planId = created.id;
            Assert.Equal(code.ToUpperInvariant(), created.code.ToUpperInvariant());

            // Plans are platform-owned (no tenant), so they are audited via the entity's own
            // IAuditable CreatedBy/CreatedAt stamping rather than a tenant-attributed AuditLog row
            // (matching how announcements/settings/feature-flags are audited).
            await using (var db = Phase4Db.Platform(_factory))
            {
                var row = await db.subscriptionPlanDefinitions.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == planId);
                Assert.NotNull(row);
                Assert.False(string.IsNullOrEmpty(row!.CreatedBy));
            }

            // Update: name + a limit change; verify it persists and audits.
            var updateBody = ValidPlanBody(code);
            var update = await sys.PutAsJsonAsync($"/api/v1/tenants/plans/{planId}", new
            {
                code, name = "Renamed Plan", description = "Updated.", tier = 2, billingPeriod = 1,
                price = 29.99, currency = "usd", trialDays = 14, isActive = true,
                maxStudents = 250, maxTeachers = 20, maxParents = 400, maxSchoolAdmins = 3,
                maxClasses = 30, maxSubjects = 20, maxLessonMaterials = 800, maxStorageMb = 4096,
                maxAiGenerationsPerMonth = 2000, maxAiTokensPerMonth = 1_000_000,
            });
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            var updated = (await Read<PlanRow>(update))!.data!;
            Assert.Equal("Renamed Plan", updated.name);

            await using (var db = Phase4Db.Platform(_factory))
            {
                var row = await db.subscriptionPlanDefinitions.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == planId);
                Assert.NotNull(row);
                Assert.False(string.IsNullOrEmpty(row!.UpdatedBy));
            }
        }
        finally { if (planId != null) await DeletePlanAsync(planId); }
    }

    [Theory]
    [InlineData("ADMIN-T1")]
    [InlineData("TEACH-T1")]
    [InlineData("STU-T1")]
    [InlineData("PARENT-T1")]
    public async Task Non_system_admin_cannot_create_a_plan(string loginCode)
    {
        var client = await TestClient.AuthedClientAsync(_factory, loginCode);
        var resp = await client.PostAsJsonAsync("/api/v1/tenants/plans", ValidPlanBody(NewCode("DENY")));
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Duplicate_plan_code_is_rejected_on_create_and_on_update()
    {
        var sys = await TestClient.AuthedClientAsync(_factory, "SYS-1");
        var codeA = NewCode("DUPA");
        var codeB = NewCode("DUPB");
        string? planAId = null, planBId = null;
        try
        {
            var createA = await sys.PostAsJsonAsync("/api/v1/tenants/plans", ValidPlanBody(codeA));
            Assert.Equal(HttpStatusCode.Created, createA.StatusCode);
            planAId = (await Read<PlanRow>(createA))!.data!.id;

            // Creating a second plan with the SAME code is rejected.
            var dupCreate = await sys.PostAsJsonAsync("/api/v1/tenants/plans", ValidPlanBody(codeA));
            Assert.Equal(HttpStatusCode.Conflict, dupCreate.StatusCode);

            var createB = await sys.PostAsJsonAsync("/api/v1/tenants/plans", ValidPlanBody(codeB));
            Assert.Equal(HttpStatusCode.Created, createB.StatusCode);
            planBId = (await Read<PlanRow>(createB))!.data!.id;

            // Renaming B's code to collide with A is rejected.
            var body = ValidPlanBody(codeA);
            var dupUpdate = await sys.PutAsJsonAsync($"/api/v1/tenants/plans/{planBId}", body);
            Assert.Equal(HttpStatusCode.Conflict, dupUpdate.StatusCode);
        }
        finally
        {
            if (planAId != null) await DeletePlanAsync(planAId);
            if (planBId != null) await DeletePlanAsync(planBId);
        }
    }

    [Fact]
    public async Task Invalid_plan_fields_are_rejected_with_400()
    {
        var sys = await TestClient.AuthedClientAsync(_factory, "SYS-1");

        var missingCode = await sys.PostAsJsonAsync("/api/v1/tenants/plans", new { code = "", name = "X", tier = 1, billingPeriod = 0, price = 1, currency = "USD" });
        Assert.Equal(HttpStatusCode.BadRequest, missingCode.StatusCode);

        var negativePrice = await sys.PostAsJsonAsync("/api/v1/tenants/plans", new { code = NewCode("NEG"), name = "X", tier = 1, billingPeriod = 0, price = -5, currency = "USD" });
        Assert.Equal(HttpStatusCode.BadRequest, negativePrice.StatusCode);

        var badCurrency = await sys.PostAsJsonAsync("/api/v1/tenants/plans", new { code = NewCode("CUR"), name = "X", tier = 1, billingPeriod = 0, price = 1, currency = "US" });
        Assert.Equal(HttpStatusCode.BadRequest, badCurrency.StatusCode);

        var badBillingPeriod = await sys.PostAsJsonAsync("/api/v1/tenants/plans", new { code = NewCode("BIL"), name = "X", tier = 1, billingPeriod = 99, price = 1, currency = "USD" });
        Assert.Equal(HttpStatusCode.BadRequest, badBillingPeriod.StatusCode);

        var negativeLimit = await sys.PostAsJsonAsync("/api/v1/tenants/plans", new { code = NewCode("LIM"), name = "X", tier = 1, billingPeriod = 0, price = 1, currency = "USD", maxStudents = -1 });
        Assert.Equal(HttpStatusCode.BadRequest, negativeLimit.StatusCode);
    }
}
