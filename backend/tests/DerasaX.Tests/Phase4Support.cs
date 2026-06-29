using System;
using System.Threading.Tasks;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Domain.Entities.Models;
using DerasaX.Infrastructure.DbHelper.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DerasaX.Tests;

/// <summary>
/// Shared helpers for Phase 4 domain/database tests: a controllable tenant context
/// and direct DbContext access against the real local PostgreSQL database.
/// </summary>
public sealed class StubTenantContext : ITenantContext
{
    public string? TenantId { get; init; }
    public bool IsPlatformScope { get; init; }
    public string? UserId { get; init; }
    public string? Role { get; init; }
    public bool HasTenant => TenantId != null;
    public bool IsAuthenticated => true;
}

public static class Phase4Db
{
    public static DerasaXDbContext NewContext(IntegrationFactory factory, ITenantContext tenant)
    {
        var cs = factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("cs");
        var options = new DbContextOptionsBuilder<DerasaXDbContext>().UseNpgsql(cs).Options;
        return new DerasaXDbContext(options, tenant);
    }

    public static DerasaXDbContext Platform(IntegrationFactory factory, string? userId = null) =>
        NewContext(factory, new StubTenantContext { IsPlatformScope = true, UserId = userId });

    public static DerasaXDbContext AsTenant(IntegrationFactory factory, string tenantId, string? userId = null) =>
        NewContext(factory, new StubTenantContext { TenantId = tenantId, UserId = userId });

    /// <summary>Ensures a tenant row exists (uses platform scope to bypass the tenant filter).</summary>
    public static async Task EnsureTenantAsync(IntegrationFactory factory, string id)
    {
        await using var db = Platform(factory);
        if (!await db.tenants.IgnoreQueryFilters().AnyAsync(t => t.Id == id))
        {
            db.tenants.Add(new Tenant { Id = id, Name = id, Status = DerasaX.Domain.Enums.TenantStatus.Active });
            await db.SaveChangesAsync();
        }
    }

    public static string NewId(string prefix) => $"{prefix}-{Guid.NewGuid():N}";
}
