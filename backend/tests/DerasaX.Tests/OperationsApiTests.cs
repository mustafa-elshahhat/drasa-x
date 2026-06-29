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
/// Phase 5 §14 (Increment 7) — tenant &amp; operations APIs through the real HTTP pipeline:
/// strict SystemAdmin vs SchoolAdmin separation, tenant lifecycle + the suspended-tenant gate,
/// subscription assignment + renewal, support lifecycle, audit isolation/privilege, AI-usage
/// recording, secret-setting redaction, feature-flag authorization, file metadata isolation and
/// report date-bounds validation.
/// </summary>
public class OperationsApiTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public OperationsApiTests(IntegrationFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private sealed record Env<T>(bool success, int statusCode, T? data, int totalCount);
    private sealed record IdRow(string id);
    private sealed record TenantRow(string id, int status);
    private sealed record SettingRow(string id, string key, string value, bool isSecret);
    private sealed record EvalRow(string key, bool enabled);

    private static string NewId(string p) => $"{p}-{Guid.NewGuid():N}"[..18];

    private static async Task<Env<T>?> Read<T>(HttpResponseMessage r)
    {
        var raw = await r.Content.ReadAsStringAsync();
        return (!string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith("{")) ? JsonSerializer.Deserialize<Env<T>>(raw, Json) : null;
    }

    private async Task<string> InsertPlanAsync()
    {
        var planId = NewId("plan");
        await using var db = Phase4Db.Platform(_factory);
        db.subscriptionPlanDefinitions.Add(new SubscriptionPlanDefinition
        {
            Id = planId, Code = NewId("PL")[..12], Name = "Pro", Tier = SubscriptionPlan.Pro,
            BillingPeriod = BillingPeriod.Monthly, Price = 10, Currency = "USD", MaxStudents = 1000,
            MaxAiGenerationsPerMonth = 500, IsActive = true
        });
        await db.SaveChangesAsync();
        return planId;
    }

    private async Task DeletePlanAsync(string planId)
    {
        await using var db = Phase4Db.Platform(_factory);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"subscriptionPlanDefinitions\" WHERE \"Id\" = {0}", planId);
    }

    // ---- Authorization separation ----

    [Fact]
    public async Task SystemAdmin_and_SchoolAdmin_routes_are_separated()
    {
        var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var sys = await TestClient.AuthedClientAsync(_factory, "SYS-1");

        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/v1/tenants")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await sys.GetAsync("/api/v1/my-tenant")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await sys.GetAsync("/api/v1/tenants")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/my-tenant")).StatusCode);
    }

    [Fact]
    public async Task Tenant_lifecycle_subscription_and_suspended_gate()
    {
        var sys = await TestClient.AuthedClientAsync(_factory, "SYS-1");
        var tenantId = NewId("t");
        var planId = await InsertPlanAsync();
        try
        {
            var create = await sys.PostAsJsonAsync("/api/v1/tenants", new { id = tenantId, name = "Throwaway", type = 0 });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);

            var assign = await sys.PostAsJsonAsync("/api/v1/tenants/subscriptions", new { tenantId, planDefinitionId = planId, isTrial = false });
            Assert.Equal(HttpStatusCode.Created, assign.StatusCode);

            var sub = await sys.GetAsync($"/api/v1/tenants/{tenantId}/subscription");
            Assert.Equal(HttpStatusCode.OK, sub.StatusCode);

            var suspend = await sys.PostAsync($"/api/v1/tenants/{tenantId}/suspend", null);
            var (ss, sb) = (suspend.StatusCode, await Read<TenantRow>(suspend));
            Assert.Equal(HttpStatusCode.OK, ss);
            Assert.Equal((int)TenantStatus.Suspended, sb!.data!.status);

            // The suspended-tenant gate (Phase 3): a member of a suspended tenant cannot log in.
            var (loginStatus, _) = await TestClient.LoginAsync(TestClient.NewClient(_factory), "STU-SUS");
            Assert.Equal((int)HttpStatusCode.Forbidden, loginStatus);
        }
        finally
        {
            await using var db = Phase4Db.Platform(_factory);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"auditLogs\" WHERE \"TenantId\" = {0}", tenantId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"subscriptionRenewals\" WHERE \"TenantId\" = {0}", tenantId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"tenantSubscriptions\" WHERE \"TenantId\" = {0}", tenantId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"tenants\" WHERE \"Id\" = {0}", tenantId);
            await DeletePlanAsync(planId);
        }
    }

    [Fact]
    public async Task SchoolAdmin_self_service_usage_and_renewal()
    {
        var planId = await InsertPlanAsync();
        var subId = NewId("sub");
        await using (var db = Phase4Db.AsTenant(_factory, "tenant-1"))
        {
            db.tenantSubscriptions.Add(new TenantSubscription
            {
                Id = subId, TenantId = "tenant-1", PlanDefinitionId = planId, Status = SubscriptionStatus.Active, StartsAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        try
        {
            var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/my-tenant/usage")).StatusCode);
            var renew = await admin.PostAsJsonAsync("/api/v1/my-tenant/renewal-requests", new { notes = "please extend" });
            Assert.Equal(HttpStatusCode.Created, renew.StatusCode);
        }
        finally
        {
            await using var db = Phase4Db.Platform(_factory);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"subscriptionRenewals\" WHERE \"TenantSubscriptionId\" = {0}", subId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"tenantSubscriptions\" WHERE \"Id\" = {0}", subId);
            await DeletePlanAsync(planId);
        }
    }

    [Fact]
    public async Task Support_request_lifecycle_and_authorization()
    {
        string reqId = "";
        try
        {
            var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
            var create = await student.PostAsJsonAsync("/api/v1/support-requests", new { type = 2, message = "App is slow" });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            reqId = (await Read<IdRow>(create))!.data!.id;

            // A non-admin cannot respond.
            Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsJsonAsync($"/api/v1/support-requests/{reqId}/respond", new { responseMessage = "x", status = 4 })).StatusCode);

            var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
            var respond = await admin.PostAsJsonAsync($"/api/v1/support-requests/{reqId}/respond", new { responseMessage = "Looking into it", status = (int)RequestStatus.Completed });
            Assert.Equal(HttpStatusCode.OK, respond.StatusCode);
        }
        finally
        {
            await using var db = Phase4Db.Platform(_factory);
            if (reqId != "") await db.Database.ExecuteSqlRawAsync("DELETE FROM \"supportRequests\" WHERE \"Id\" = {0}", reqId);
        }
    }

    [Fact]
    public async Task Audit_isolation_and_platform_privilege()
    {
        var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var student = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var sys = await TestClient.AuthedClientAsync(_factory, "SYS-1");

        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/audit?pageSize=5")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync("/api/v1/audit")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await sys.GetAsync("/api/v1/platform-audit?pageSize=5")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/v1/platform-audit")).StatusCode);
    }

    [Fact]
    public async Task AiUsage_settings_flags_files_and_report_bounds()
    {
        var admin = await TestClient.AuthedClientAsync(_factory, "ADMIN-T1");
        var sys = await TestClient.AuthedClientAsync(_factory, "SYS-1");
        var settingKey = NewId("k");
        var flagKey = NewId("flag");
        string fileId = "", aiId = "";
        try
        {
            // AI usage recording + queries (no AI executed).
            var rec = await admin.PostAsJsonAsync("/api/v1/ai-usage", new { kind = 1, provider = "groq", model = "x", totalTokens = 100, cost = 0.01, failed = false, latencyMs = 250 });
            Assert.Equal(HttpStatusCode.Created, rec.StatusCode);
            aiId = (await Read<IdRow>(rec))!.data!.id;
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/ai-usage?pageSize=5")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/ai-usage/summary")).StatusCode);

            // Secret tenant setting is redacted on read.
            Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync("/api/v1/tenant-settings", new { key = settingKey, value = "s3cr3t", valueType = 0, isSecret = true })).StatusCode);
            var settings = await Read<List<SettingRow>>(await admin.GetAsync("/api/v1/tenant-settings"));
            var row = settings!.data!.First(s => s.key == settingKey);
            Assert.True(row.isSecret);
            Assert.DoesNotContain("s3cr3t", row.value);

            // Platform settings/flags are SystemAdmin-only.
            Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/v1/system-settings")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await sys.PutAsJsonAsync("/api/v1/system-settings", new { key = settingKey, value = "v", valueType = 0, isSecret = false })).StatusCode);

            Assert.Equal(HttpStatusCode.OK, (await sys.PutAsJsonAsync("/api/v1/feature-flags", new { key = flagKey, isEnabled = true, targetTenantId = (string?)null })).StatusCode);
            var eval = await Read<EvalRow>(await admin.GetAsync($"/api/v1/feature-flags/{flagKey}/evaluate"));
            Assert.True(eval!.data!.enabled);

            // File metadata isolation.
            var file = await admin.PostAsJsonAsync("/api/v1/files", new { fileName = "a.pdf", contentType = "application/pdf", sizeBytes = 1024, type = 4 });
            Assert.Equal(HttpStatusCode.Created, file.StatusCode);
            fileId = (await Read<IdRow>(file))!.data!.id;
            var admin2 = await TestClient.AuthedClientAsync(_factory, "ADMIN-T2");
            Assert.Equal(HttpStatusCode.NotFound, (await admin2.GetAsync($"/api/v1/files/{fileId}")).StatusCode);

            // Report date-range validation.
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/v1/reports/assessment-summary?from=2031-01-01&to=2030-01-01")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/reports/tenant-users")).StatusCode);
        }
        finally
        {
            await using var db = Phase4Db.Platform(_factory);
            if (aiId != "") await db.Database.ExecuteSqlRawAsync("DELETE FROM \"auditLogs\" WHERE \"EntityId\" = {0}", aiId);
            if (aiId != "") await db.Database.ExecuteSqlRawAsync("DELETE FROM \"aiUsageRecords\" WHERE \"Id\" = {0}", aiId);
            if (fileId != "") await db.Database.ExecuteSqlRawAsync("DELETE FROM \"fileRecords\" WHERE \"Id\" = {0}", fileId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"tenantSettings\" WHERE \"Key\" = {0}", settingKey);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"systemSettings\" WHERE \"Key\" = {0}", settingKey);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"featureFlags\" WHERE \"Key\" = {0}", flagKey);
        }
    }
}
