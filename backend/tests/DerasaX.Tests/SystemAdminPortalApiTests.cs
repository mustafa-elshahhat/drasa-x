using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 12 — System Admin (platform) Portal API authorization + behaviour matrix for the NEW
/// platform contracts (aggregate dashboard, platform usage/AI/storage roll-ups, cross-tenant support
/// inbox, durable platform announcements, create-initial-school-admin, operational status, and the
/// SAFE non-destructive tenant data export/deletion request). Proves: real platform aggregate, the
/// full create-tenant → assign-plan → create-admin → activate → suspend → reactivate lifecycle with
/// the suspended-tenant login gate, audit coverage, validation/conflict paths, non-destructive data
/// workflow, and the wrong-role (403) / unauthenticated (401) gate. Uses the always-seeded SYS-1
/// platform actor and self-contained data (create-then-clean-up) so the suite is repeatable without a
/// re-seed.
/// </summary>
public class SystemAdminPortalApiTests : IClassFixture<IntegrationFactory>
{
    private const string Sys = "SYS-1";
    private readonly IntegrationFactory _factory;
    public SystemAdminPortalApiTests(IntegrationFactory factory) => _factory = factory;

    private static string NewId(string p) => $"{p}-{Guid.NewGuid():N}"[..20];

    private static async Task<JsonElement> DataAsync(HttpResponseMessage resp)
    {
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("data").Clone();
    }

    private async Task<string> InsertPlanAsync()
    {
        var planId = NewId("plan");
        await using var db = Phase4Db.Platform(_factory);
        db.subscriptionPlanDefinitions.Add(new SubscriptionPlanDefinition
        {
            Id = planId, Code = NewId("PL")[..12], Name = "Pro", Tier = SubscriptionPlan.Pro,
            BillingPeriod = BillingPeriod.Monthly, Price = 10, Currency = "USD", MaxStudents = 1000,
            MaxStorageMb = 2048, MaxAiGenerationsPerMonth = 500, IsActive = true
        });
        await db.SaveChangesAsync();
        return planId;
    }

    private async Task CleanupTenantAsync(string tenantId, string? userId, string planId)
    {
        try
        {
            if (userId is not null)
            {
                using var scope = _factory.Services.CreateScope();
                var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var u = await um.FindByIdAsync(userId);
                if (u is not null) await um.DeleteAsync(u);
            }
            await using var db = Phase4Db.Platform(_factory);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"auditLogs\" WHERE \"TenantId\" = {0}", tenantId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"subscriptionRenewals\" WHERE \"TenantId\" = {0}", tenantId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"tenantSubscriptions\" WHERE \"TenantId\" = {0}", tenantId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"tenants\" WHERE \"Id\" = {0}", tenantId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"subscriptionPlanDefinitions\" WHERE \"Id\" = {0}", planId);
        }
        catch { /* best-effort cleanup; unique ids keep the test repeatable regardless */ }
    }

    // ---- dashboard / usage / ai / storage: real platform aggregates ----

    [Fact]
    public async Task SystemAdmin_dashboard_returns_real_platform_aggregate()
    {
        var sys = await TestClient.AuthedClientAsync(_factory, Sys);
        var resp = await sys.GetAsync("/api/v1/system-admin/dashboard");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var d = await DataAsync(resp);
        // Real counts from authoritative data — at least tenant-1, tenant-2 and the suspended tenant exist.
        Assert.True(d.GetProperty("tenantsTotal").GetInt32() >= 2);
        Assert.True(d.GetProperty("tenantsActive").GetInt32() >= 2);
        Assert.True(d.GetProperty("students").GetInt32() >= 1);
        Assert.True(d.GetProperty("teachers").GetInt32() >= 1);
        Assert.True(d.GetProperty("systemAdmins").GetInt32() >= 1);
    }

    [Fact]
    public async Task Platform_usage_ai_and_storage_return_real_data()
    {
        var sys = await TestClient.AuthedClientAsync(_factory, Sys);

        var usage = await sys.GetAsync("/api/v1/system-admin/usage");
        Assert.Equal(HttpStatusCode.OK, usage.StatusCode);
        var u = await DataAsync(usage);
        Assert.True(u.GetProperty("tenantsCount").GetInt32() >= 2);
        Assert.True(u.GetProperty("tenants").GetArrayLength() >= 2);

        var ai = await sys.GetAsync("/api/v1/system-admin/ai-usage");
        Assert.Equal(HttpStatusCode.OK, ai.StatusCode);

        var storage = await sys.GetAsync("/api/v1/system-admin/storage");
        Assert.Equal(HttpStatusCode.OK, storage.StatusCode);
        // Honest posture: byte accounting is NOT implemented yet.
        Assert.False((await DataAsync(storage)).GetProperty("byteAccountingImplemented").GetBoolean());
    }

    [Fact]
    public async Task SystemAdmin_can_list_tenants_via_reused_contract()
    {
        var sys = await TestClient.AuthedClientAsync(_factory, Sys);
        var resp = await sys.GetAsync("/api/v1/tenants");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("Main School", await resp.Content.ReadAsStringAsync());
    }

    // ---- full onboarding + lifecycle + audit (the canonical end-to-end) ----

    [Fact]
    public async Task Onboard_assign_plan_create_admin_then_suspend_blocks_and_reactivate_restores()
    {
        var sys = await TestClient.AuthedClientAsync(_factory, Sys);
        var tenantId = NewId("ten");
        var adminCode = NewId("ADM");
        var planId = await InsertPlanAsync();
        string? adminUserId = null;
        try
        {
            // 1) Create tenant (created Active).
            var create = await sys.PostAsJsonAsync("/api/v1/tenants", new { id = tenantId, name = "Phase12 Onboarded", type = 0 });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);

            // 2) Assign a plan.
            var assign = await sys.PostAsJsonAsync("/api/v1/tenants/subscriptions",
                new { tenantId, planDefinitionId = planId, isTrial = false });
            Assert.Equal(HttpStatusCode.Created, assign.StatusCode);

            // 3) Create the INITIAL school admin for the tenant (NEW Phase 12 contract).
            var mk = await sys.PostAsJsonAsync($"/api/v1/system-admin/tenants/{tenantId}/school-admins",
                new { fullName = "Phase12 Founder Admin", loginCode = adminCode });
            Assert.Equal(HttpStatusCode.Created, mk.StatusCode);
            var mkd = await DataAsync(mk);
            adminUserId = mkd.GetProperty("userId").GetString();
            var tempPassword = mkd.GetProperty("temporaryPassword").GetString();
            Assert.Equal(tenantId, mkd.GetProperty("tenantId").GetString());      // correct tenant
            Assert.Equal("SchoolAdmin", mkd.GetProperty("role").GetString());      // correct role
            Assert.False(string.IsNullOrWhiteSpace(tempPassword));

            // 4) The new admin can log in while the tenant is Active, and is a SchoolAdmin.
            var (st1, b1) = await TestClient.LoginAsync(TestClient.NewClient(_factory), adminCode, tempPassword);
            Assert.Equal(200, st1);
            Assert.True(b1!.isAuthenticated);
            Assert.Equal("SchoolAdmin", b1.role);

            // 5) Suspend → the tenant's users can no longer log in (Phase 3 suspended-tenant gate).
            var suspend = await sys.PostAsync($"/api/v1/tenants/{tenantId}/suspend", null);
            Assert.Equal(HttpStatusCode.OK, suspend.StatusCode);
            var (st2, _) = await TestClient.LoginAsync(TestClient.NewClient(_factory), adminCode, tempPassword);
            Assert.Equal(403, st2);

            // 6) Reactivate → login is restored.
            var reactivate = await sys.PostAsync($"/api/v1/tenants/{tenantId}/reactivate", null);
            Assert.Equal(HttpStatusCode.OK, reactivate.StatusCode);
            var (st3, b3) = await TestClient.LoginAsync(TestClient.NewClient(_factory), adminCode, tempPassword);
            Assert.Equal(200, st3);
            Assert.True(b3!.isAuthenticated);

            // 7) The lifecycle mutations are recorded in the real platform audit trail for this tenant.
            var audit = await sys.GetAsync("/api/v1/platform-audit?pageSize=100");
            Assert.Equal(HttpStatusCode.OK, audit.StatusCode);
            Assert.Contains(tenantId, await audit.Content.ReadAsStringAsync());
        }
        finally { await CleanupTenantAsync(tenantId, adminUserId, planId); }
    }

    // ---- onboarding negative paths ----

    [Fact]
    public async Task Create_tenant_duplicate_id_conflict()
    {
        var sys = await TestClient.AuthedClientAsync(_factory, Sys);
        var resp = await sys.PostAsJsonAsync("/api/v1/tenants", new { id = "tenant-1", name = "Dup", type = 0 });
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task Create_school_admin_duplicate_login_code_conflict()
    {
        var sys = await TestClient.AuthedClientAsync(_factory, Sys);
        // ADMIN-T1 is an existing login code → 409 (login code is the global authentication key).
        var resp = await sys.PostAsJsonAsync("/api/v1/system-admin/tenants/tenant-1/school-admins",
            new { fullName = "Clash", loginCode = "ADMIN-T1" });
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task Create_school_admin_unknown_tenant_404()
    {
        var sys = await TestClient.AuthedClientAsync(_factory, Sys);
        var resp = await sys.PostAsJsonAsync($"/api/v1/system-admin/tenants/{NewId("nope")}/school-admins",
            new { fullName = "Ghost", loginCode = NewId("GH") });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Create_school_admin_missing_fields_400()
    {
        var sys = await TestClient.AuthedClientAsync(_factory, Sys);
        var resp = await sys.PostAsJsonAsync("/api/v1/system-admin/tenants/tenant-1/school-admins",
            new { fullName = "", loginCode = "" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ---- cross-tenant support inbox ----

    [Fact]
    public async Task Support_inbox_lists_cross_tenant_and_platform_admin_can_respond()
    {
        // A tenant-1 student raises a support ticket through the tenant-member contract.
        var stu = await TestClient.AuthedClientAsync(_factory, "STU-T1");
        var raise = await stu.PostAsJsonAsync("/api/v1/support-requests", new { type = 2, message = "Phase12 inbox test" });
        Assert.Equal(HttpStatusCode.Created, raise.StatusCode);
        var ticketId = (await DataAsync(raise)).GetProperty("id").GetString()!;
        try
        {
            var sys = await TestClient.AuthedClientAsync(_factory, Sys);

            // The platform inbox sees the cross-tenant ticket.
            var list = await sys.GetAsync("/api/v1/system-admin/support-tickets?pageSize=100");
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            Assert.Contains(ticketId, await list.Content.ReadAsStringAsync());

            // The tenant filter is honoured (cross-tenant scoping is explicit, not leaked).
            var filtered = await sys.GetAsync("/api/v1/system-admin/support-tickets?tenantId=tenant-2&pageSize=100");
            Assert.Equal(HttpStatusCode.OK, filtered.StatusCode);
            Assert.DoesNotContain(ticketId, await filtered.Content.ReadAsStringAsync());

            // The platform admin responds and drives the status.
            var respond = await sys.PostAsJsonAsync($"/api/v1/system-admin/support-tickets/{ticketId}/respond",
                new { responseMessage = "Handled by platform.", status = 4 });
            Assert.Equal(HttpStatusCode.OK, respond.StatusCode);
            Assert.Equal((int)RequestStatus.Completed, (await DataAsync(respond)).GetProperty("status").GetInt32());
        }
        finally
        {
            await using var db = Phase4Db.Platform(_factory);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"notifications\" WHERE \"TenantId\" = {0} AND \"Title\" = 'Support request updated'", "tenant-1");
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"auditLogs\" WHERE \"EntityType\" = 'SupportRequest' AND \"EntityId\" = {0}", ticketId);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"supportRequests\" WHERE \"Id\" = {0}", ticketId);
        }
    }

    // ---- platform announcements ----

    [Fact]
    public async Task Platform_announcement_create_then_appears_in_list()
    {
        var sys = await TestClient.AuthedClientAsync(_factory, Sys);
        var title = "Phase12 " + NewId("ann");
        var create = await sys.PostAsJsonAsync("/api/v1/system-admin/announcements",
            new { title, body = "A real platform-wide announcement." });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var list = await sys.GetAsync("/api/v1/system-admin/announcements");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains(title, await list.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Platform_announcement_missing_fields_400()
    {
        var sys = await TestClient.AuthedClientAsync(_factory, Sys);
        var resp = await sys.PostAsJsonAsync("/api/v1/system-admin/announcements", new { title = "", body = "" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ---- operational status: real health + honest deferrals ----

    [Fact]
    public async Task Operational_status_reports_real_health_and_honest_deferrals()
    {
        var sys = await TestClient.AuthedClientAsync(_factory, Sys);
        var resp = await sys.GetAsync("/api/v1/system-admin/operational-status");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var d = await DataAsync(resp);
        Assert.True(d.GetProperty("health").GetProperty("databaseReachable").GetBoolean()); // real DB read
        Assert.False(d.GetProperty("errorMonitoring").GetProperty("configured").GetBoolean()); // honestly deferred
        Assert.False(d.GetProperty("backups").GetProperty("configured").GetBoolean());          // honestly deferred
    }

    // ---- SAFE, non-destructive tenant data workflow ----

    [Fact]
    public async Task Tenant_data_export_is_a_non_destructive_preview()
    {
        var sys = await TestClient.AuthedClientAsync(_factory, Sys);
        var resp = await sys.PostAsync("/api/v1/system-admin/tenants/tenant-1/data-export", null);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var d = await DataAsync(resp);
        Assert.False(d.GetProperty("destructive").GetBoolean());
        Assert.True(d.GetProperty("preview").GetProperty("students").GetInt32() >= 1);

        // Non-destructive: tenant-1 still exists and still has its users afterwards.
        Assert.Equal(HttpStatusCode.OK, (await sys.GetAsync("/api/v1/tenants/tenant-1")).StatusCode);
        var usage = await DataAsync(await sys.GetAsync("/api/v1/system-admin/usage"));
        Assert.True(usage.GetProperty("totalStudents").GetInt32() >= 1);
    }

    [Fact]
    public async Task Tenant_deletion_request_records_but_never_deletes()
    {
        var sys = await TestClient.AuthedClientAsync(_factory, Sys);
        var resp = await sys.PostAsync("/api/v1/system-admin/tenants/tenant-1/data-deletion-request", null);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var d = await DataAsync(resp);
        Assert.False(d.GetProperty("destructive").GetBoolean());
        Assert.Equal("request-recorded", d.GetProperty("status").GetString());

        // tenant-1 must remain fully intact (no destructive deletion in this phase).
        var tenant = await DataAsync(await sys.GetAsync("/api/v1/tenants/tenant-1"));
        Assert.Equal((int)TenantStatus.Active, tenant.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Data_export_unknown_tenant_404()
    {
        var sys = await TestClient.AuthedClientAsync(_factory, Sys);
        var resp = await sys.PostAsync($"/api/v1/system-admin/tenants/{NewId("nope")}/data-export", null);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ---- wrong role (403) / unauthenticated (401) on the system-admin surface ----

    [Theory]
    [InlineData("STU-T1")]    // Student
    [InlineData("TEACH-T1")]  // Teacher
    [InlineData("PARENT-T1")] // Parent
    [InlineData("ADMIN-T1")]  // SchoolAdmin is NOT a platform SystemAdmin
    public async Task Non_system_admin_is_denied_403(string loginCode)
    {
        var client = await TestClient.AuthedClientAsync(_factory, loginCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/system-admin/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/system-admin/support-tickets")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/system-admin/announcements", new { title = "x", body = "y" })).StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_is_denied_401()
    {
        var client = TestClient.NewClient(_factory);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/system-admin/dashboard")).StatusCode);
    }
}
