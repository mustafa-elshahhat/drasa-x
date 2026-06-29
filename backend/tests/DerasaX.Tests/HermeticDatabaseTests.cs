using System;
using System.Linq;
using System.Threading.Tasks;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Infrastructure.DbHelper.Context;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 22 Step 5 — HERMETIC integration tests.
///
/// Unlike <see cref="FreshDatabaseMigrationTests"/> (which targets the developer's NATIVE local
/// PostgreSQL on :5432), these run against a DISPOSABLE PostgreSQL container started by
/// Testcontainers. They depend on NOTHING in the local <c>derasax_local</c> database: the
/// container starts empty, every migration is applied from zero, each test creates its own
/// isolated data, and the whole database is destroyed when the container is disposed. This proves
/// the migration set + core tenant-isolation behaviour from a clean checkout with only Docker
/// available — no manual <c>reset-local-db</c>/<c>start-local</c> seeding required.
///
/// REQUIRES DOCKER. The tests are categorised <c>Hermetic</c> so a Docker-less environment can
/// exclude them with <c>dotnet test --filter "Category!=Hermetic"</c>; the rest of the suite (which
/// uses the seeded native DB) is unaffected. The native <c>reset-local-db</c> path remains the
/// operational/local-stack check, NOT the hermetic integration-test proof.
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    // Pinned to the same major version as docker-compose.yml and the dev DB so behaviour matches.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .Build();

    // Pooling=false so no connection lingers to block container disposal.
    public string ConnectionString => _container.GetConnectionString() + ";Pooling=false";

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Apply EVERY migration to the brand-new container database (from empty).
        var options = new DbContextOptionsBuilder<DerasaXDbContext>().UseNpgsql(ConnectionString).Options;
        await using var db = new DerasaXDbContext(options, new StubTenantContext { IsPlatformScope = true });
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[Trait("Category", "Hermetic")]
public sealed class HermeticDatabaseTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fx;
    public HermeticDatabaseTests(PostgresContainerFixture fx) => _fx = fx;

    private DerasaXDbContext NewContext(ITenantContext tenant) =>
        new(new DbContextOptionsBuilder<DerasaXDbContext>().UseNpgsql(_fx.ConnectionString).Options, tenant);

    [Fact]
    public async Task Every_migration_applies_from_zero_on_an_ephemeral_container()
    {
        await using var db = NewContext(new StubTenantContext { IsPlatformScope = true });

        // The full migration set must already be applied to the fresh container DB, with nothing pending.
        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
        Assert.True(applied.Count >= 11, $"expected >=11 migrations applied, got {applied.Count}");
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task Tenant_query_filter_isolates_rows_on_the_ephemeral_database()
    {
        var tenantA = $"tc-a-{Guid.NewGuid():N}";
        var tenantB = $"tc-b-{Guid.NewGuid():N}";

        // Seed two tenants, each with one Grade, via platform scope (bypasses the tenant query filter).
        await using (var seed = NewContext(new StubTenantContext { IsPlatformScope = true }))
        {
            seed.tenants.Add(new Tenant { Id = tenantA, Name = tenantA, Status = TenantStatus.Active });
            seed.tenants.Add(new Tenant { Id = tenantB, Name = tenantB, Status = TenantStatus.Active });
            seed.Set<Grade>().Add(new Grade { Id = Guid.NewGuid().ToString(), Name = "GA", TenantId = tenantA });
            seed.Set<Grade>().Add(new Grade { Id = Guid.NewGuid().ToString(), Name = "GB", TenantId = tenantB });
            await seed.SaveChangesAsync();
        }

        // As tenant A, the global query filter must return ONLY tenant A's grade (GB stays hidden).
        await using (var asTenantA = NewContext(new StubTenantContext { TenantId = tenantA }))
        {
            var grades = await asTenantA.Set<Grade>().ToListAsync();
            Assert.Single(grades);
            Assert.Equal("GA", grades[0].Name);
            Assert.Equal(tenantA, grades[0].TenantId);
        }
    }
}
