using System;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>Phase 4 §6.1 — tenant &amp; subscription domain/database integrity.</summary>
public class SubscriptionDomainTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public SubscriptionDomainTests(IntegrationFactory factory) => _factory = factory;

    private async Task<SubscriptionPlanDefinition> CreatePlanAsync(string code)
    {
        var plan = new SubscriptionPlanDefinition
        {
            Id = Phase4Db.NewId("plan"),
            Code = code,
            Name = "Test Plan",
            Tier = SubscriptionPlan.Pro,
            Price = 9.99m,
            MaxStudents = 100
        };
        await using var db = Phase4Db.Platform(_factory);
        db.subscriptionPlanDefinitions.Add(plan);
        await db.SaveChangesAsync();
        return plan;
    }

    [Fact]
    public async Task TenantSubscription_requires_tenant()
    {
        var plan = await CreatePlanAsync(Phase4Db.NewId("CODE"));
        try
        {
            await using var db = Phase4Db.Platform(_factory);
            // A required, FK-backed and key-bearing tenant means the row cannot even be
            // tracked/persisted with a null tenant: EF rejects it at Add (alternate-key
            // null) or, where no AK applies, at SaveChanges (NOT NULL / FK).
            var ex = await Record.ExceptionAsync(async () =>
            {
                db.tenantSubscriptions.Add(new TenantSubscription
                {
                    Id = Phase4Db.NewId("sub"),
                    TenantId = null!, // missing tenant
                    PlanDefinitionId = plan.Id,
                    StartsAt = DateTime.UtcNow,
                    Status = SubscriptionStatus.Trial
                });
                await db.SaveChangesAsync();
            });
            Assert.True(ex is DbUpdateException or InvalidOperationException,
                $"Expected persistence to fail without a tenant, got: {ex?.GetType().Name ?? "no exception"}");
        }
        finally { await CleanupPlanAsync(plan.Id); }
    }

    [Fact]
    public async Task Cross_tenant_renewal_is_rejected_and_same_tenant_succeeds()
    {
        await Phase4Db.EnsureTenantAsync(_factory, "tenant-1");
        await Phase4Db.EnsureTenantAsync(_factory, "tenant-2");
        var plan = await CreatePlanAsync(Phase4Db.NewId("CODE"));
        var subId = Phase4Db.NewId("sub");

        try
        {
            await using (var db = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                db.tenantSubscriptions.Add(new TenantSubscription
                {
                    Id = subId,
                    TenantId = "tenant-1",
                    PlanDefinitionId = plan.Id,
                    StartsAt = DateTime.UtcNow,
                    Status = SubscriptionStatus.Active
                });
                await db.SaveChangesAsync();
            }

            // Cross-tenant renewal: tenant-2 renewal pointing at tenant-1's subscription
            // must be rejected by the same-tenant composite FK.
            await using (var bad = Phase4Db.AsTenant(_factory, "tenant-2"))
            {
                bad.subscriptionRenewals.Add(new SubscriptionRenewal
                {
                    Id = Phase4Db.NewId("ren"),
                    TenantId = "tenant-2",
                    TenantSubscriptionId = subId,
                    RequestedAt = DateTime.UtcNow,
                    Status = RenewalStatus.Requested
                });
                await Assert.ThrowsAnyAsync<DbUpdateException>(() => bad.SaveChangesAsync());
            }

            // Same-tenant renewal succeeds.
            await using (var good = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                good.subscriptionRenewals.Add(new SubscriptionRenewal
                {
                    Id = Phase4Db.NewId("ren"),
                    TenantId = "tenant-1",
                    TenantSubscriptionId = subId,
                    RequestedAt = DateTime.UtcNow,
                    Status = RenewalStatus.Requested
                });
                await good.SaveChangesAsync();
                Assert.True(await good.subscriptionRenewals.AnyAsync(r => r.TenantSubscriptionId == subId));
            }
        }
        finally
        {
            await using var cleanup = Phase4Db.Platform(_factory);
            cleanup.subscriptionRenewals.RemoveRange(
                await cleanup.subscriptionRenewals.IgnoreQueryFilters().Where(r => r.TenantSubscriptionId == subId).ToListAsync());
            await cleanup.SaveChangesAsync();
            cleanup.tenantSubscriptions.RemoveRange(
                await cleanup.tenantSubscriptions.IgnoreQueryFilters().Where(s => s.Id == subId).ToListAsync());
            await cleanup.SaveChangesAsync();
            await CleanupPlanAsync(plan.Id);
        }
    }

    [Fact]
    public async Task Duplicate_plan_code_is_rejected()
    {
        var code = Phase4Db.NewId("CODE");
        var plan = await CreatePlanAsync(code);
        try
        {
            await using var db = Phase4Db.Platform(_factory);
            db.subscriptionPlanDefinitions.Add(new SubscriptionPlanDefinition
            {
                Id = Phase4Db.NewId("plan"),
                Code = code, // duplicate global code
                Name = "Dup",
                Tier = SubscriptionPlan.Free
            });
            await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        finally { await CleanupPlanAsync(plan.Id); }
    }

    [Fact]
    public async Task Plan_definition_is_platform_scoped_and_visible_to_tenant_context()
    {
        var plan = await CreatePlanAsync(Phase4Db.NewId("CODE"));
        try
        {
            // A tenant-scoped context can still read the platform plan catalog
            // (no tenant filter applies to platform-owned entities).
            await using var t1 = Phase4Db.AsTenant(_factory, "tenant-1");
            Assert.NotNull(await t1.subscriptionPlanDefinitions.FirstOrDefaultAsync(p => p.Id == plan.Id));
        }
        finally { await CleanupPlanAsync(plan.Id); }
    }

    private async Task CleanupPlanAsync(string planId)
    {
        await using var db = Phase4Db.Platform(_factory);
        db.subscriptionPlanDefinitions.RemoveRange(
            await db.subscriptionPlanDefinitions.IgnoreQueryFilters().Where(p => p.Id == planId).ToListAsync());
        await db.SaveChangesAsync();
    }
}
