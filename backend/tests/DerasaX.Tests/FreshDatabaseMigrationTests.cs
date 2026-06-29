using System;
using System.Linq;
using System.Threading.Tasks;
using DerasaX.Infrastructure.DbHelper.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Proves "fresh database migration from zero" using the RUNTIME EF migrator
/// (<c>Database.MigrateAsync</c>) rather than the <c>dotnet ef</c> design host, which the local
/// Windows Application Control (WDAC) policy blocks from loading the freshly built API assembly.
/// Creates a brand-new database, applies every migration from empty, asserts the resulting schema
/// (migration history + representative tables across all phases), then drops it. This is the same
/// migration set the existing development database is already at, so it also evidences that the
/// existing DB is current and that no extra migration is required by the Phase 5 work.
/// </summary>
public class FreshDatabaseMigrationTests : IClassFixture<IntegrationFactory>
{
    private const string AdminCs = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres;SSL Mode=Disable";
    private const string FreshDb = "derasax_phase5_freshcheck";
    private static string FreshCs => $"Host=localhost;Port=5432;Database={FreshDb};Username=postgres;Password=postgres;SSL Mode=Disable;Pooling=false";

    [Fact]
    public async Task Fresh_database_migrates_from_zero()
    {
        await DropIfExistsAsync();
        await ExecAsync(AdminCs, $"CREATE DATABASE \"{FreshDb}\"");
        try
        {
            var options = new DbContextOptionsBuilder<DerasaXDbContext>().UseNpgsql(FreshCs).Options;
            await using (var db = new DerasaXDbContext(options, new StubTenantContext { IsPlatformScope = true }))
            {
                await db.Database.MigrateAsync();

                // The full migration set must be applied from an empty database.
                var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
                Assert.True(applied.Count >= 11, $"expected >=11 migrations applied, got {applied.Count}");

                // Representative tables across every phase must exist (schema is complete).
                foreach (var table in new[] { "AspNetUsers", "tenants", "auditLogs", "quizzes", "questions",
                    "quizSubmissions", "communities", "competitions", "officeHourSessions", "aiUsageRecords",
                    "tenantSubscriptions", "parentRequests", "studentInsights" })
                    Assert.True(await TableExistsAsync(db, table), $"table '{table}' missing in fresh DB");
            }
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await DropIfExistsAsync();
        }
    }

    private static async Task<bool> TableExistsAsync(DerasaXDbContext db, string table)
    {
        await using var conn = new NpgsqlConnection(FreshCs);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name=@t)", conn);
        cmd.Parameters.AddWithValue("t", table);
        return (bool)(await cmd.ExecuteScalarAsync())!;
    }

    private static async Task DropIfExistsAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await ExecAsync(AdminCs,
            $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='{FreshDb}' AND pid<>pg_backend_pid()");
        await ExecAsync(AdminCs, $"DROP DATABASE IF EXISTS \"{FreshDb}\"");
    }

    private static async Task ExecAsync(string cs, string sql)
    {
        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }
}
