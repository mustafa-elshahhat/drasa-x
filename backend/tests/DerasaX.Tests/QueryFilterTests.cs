using System;
using System.Threading.Tasks;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Domain.Entities.Models;
using DerasaX.Infrastructure.DbHelper.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 3 closure §3.3 — the tenant-isolation filter and the soft-delete filter
/// must operate SIMULTANEOUSLY (EF Core 9 has one effective filter per entity, so
/// they are combined into a single predicate). Proves all four required cases on a
/// real PostgreSQL-backed entity (Grade).
/// </summary>
public class QueryFilterTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public QueryFilterTests(IntegrationFactory factory) => _factory = factory;

    private sealed class StubTenant : ITenantContext
    {
        public string? TenantId { get; init; }
        public bool IsPlatformScope { get; init; }
        public string? UserId => null;
        public string? Role => null;
        public bool HasTenant => TenantId != null;
        public bool IsAuthenticated => true;
    }

    private DerasaXDbContext NewContext(ITenantContext tenant)
    {
        var cs = _factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("cs");
        var options = new DbContextOptionsBuilder<DerasaXDbContext>().UseNpgsql(cs).Options;
        return new DerasaXDbContext(options, tenant);
    }

    [Fact]
    public async Task Tenant_and_softdelete_filters_apply_together()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var idA = $"QF-A-{suffix}";        // tenant-1, active
        var idADeleted = $"QF-AD-{suffix}"; // tenant-1, soft-deleted
        var idB = $"QF-B-{suffix}";        // tenant-2, active

        var platform = new StubTenant { IsPlatformScope = true };

        // Arrange — insert via an explicitly platform-scoped path.
        await using (var seed = NewContext(platform))
        {
            seed.grades.Add(new Grade { Id = idA, Name = "QF A", TenantId = "tenant-1", IsDeleted = false });
            seed.grades.Add(new Grade { Id = idADeleted, Name = "QF A Deleted", TenantId = "tenant-1", IsDeleted = true });
            seed.grades.Add(new Grade { Id = idB, Name = "QF B", TenantId = "tenant-2", IsDeleted = false });
            await seed.SaveChangesAsync();
        }

        try
        {
            // Act / Assert — as tenant-1.
            await using var t1 = NewContext(new StubTenant { TenantId = "tenant-1" });

            // (1) School A CAN read its own active entity.
            Assert.NotNull(await t1.grades.FirstOrDefaultAsync(g => g.Id == idA));

            // (2) School A CANNOT read its own soft-deleted entity.
            Assert.Null(await t1.grades.FirstOrDefaultAsync(g => g.Id == idADeleted));

            // (3) School A CANNOT read School B's active entity.
            Assert.Null(await t1.grades.FirstOrDefaultAsync(g => g.Id == idB));

            // (4) Platform scope (explicit, authorized path) sees active rows of all tenants,
            //     while soft-deleted rows still require IgnoreQueryFilters.
            await using var plat = NewContext(platform);
            Assert.NotNull(await plat.grades.FirstOrDefaultAsync(g => g.Id == idA));
            Assert.NotNull(await plat.grades.FirstOrDefaultAsync(g => g.Id == idB));
            Assert.Null(await plat.grades.FirstOrDefaultAsync(g => g.Id == idADeleted));
            Assert.NotNull(await plat.grades.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Id == idADeleted));
        }
        finally
        {
            // Cleanup — hard remove the three test rows to keep the shared DB clean.
            await using var cleanup = NewContext(platform);
            var rows = await cleanup.grades.IgnoreQueryFilters()
                .Where(g => g.Id == idA || g.Id == idADeleted || g.Id == idB)
                .ToListAsync();
            cleanup.grades.RemoveRange(rows);
            await cleanup.SaveChangesAsync();
        }
    }
}
