using System;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Infrastructure.DbHelper.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>Phase 4 §6.8 — operations domain/database integrity.</summary>
public class OperationsDomainTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public OperationsDomainTests(IntegrationFactory factory) => _factory = factory;

    private static async Task<string> UserId(DerasaXDbContext db, string loginCode) =>
        (await db.applicationUsers.IgnoreQueryFilters().FirstAsync(u => u.LoginCode == loginCode)).Id;

    [Fact]
    public async Task Operational_records_and_settings_are_persisted_with_tenant_integrity()
    {
        await using var setup = Phase4Db.Platform(_factory);
        var studentT1 = await UserId(setup, "STU-T1");
        var studentT2 = await UserId(setup, "STU-T2");
        var auditId = Phase4Db.NewId("audit");
        var fileId = Phase4Db.NewId("file");
        var usageId = Phase4Db.NewId("aiu");
        var tenantSettingId = Phase4Db.NewId("tset");
        var systemSettingId = Phase4Db.NewId("sset");
        var flagId = Phase4Db.NewId("flag");

        try
        {
            await using (var bad = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                bad.aiUsageRecords.Add(new AiUsageRecord { Id = Phase4Db.NewId("aiu"), TenantId = "tenant-1", UserId = studentT2, Kind = AiUsageKind.Chat, Provider = "test" });
                await Assert.ThrowsAnyAsync<DbUpdateException>(() => bad.SaveChangesAsync());
            }

            await using (var platform = Phase4Db.Platform(_factory))
            {
                platform.systemSettings.Add(new SystemSetting { Id = systemSettingId, Key = Phase4Db.NewId("setting"), Value = "on", ValueType = SettingValueType.String });
                platform.featureFlags.Add(new FeatureFlag { Id = flagId, Key = Phase4Db.NewId("flag"), IsEnabled = true, TargetTenantId = "tenant-1" });
                await platform.SaveChangesAsync();
            }

            await using var db = Phase4Db.AsTenant(_factory, "tenant-1");
            db.auditLogs.Add(new AuditLog { Id = auditId, TenantId = "tenant-1", ActorUserId = studentT1, Action = AuditActionType.Create, EntityType = "Smoke", EntityId = "1" });
            db.fileRecords.Add(new FileRecord { Id = fileId, TenantId = "tenant-1", UploadedByUserId = studentT1, FileName = "a.txt", ContentType = "text/plain", SizeBytes = 1, StorageKey = Phase4Db.NewId("store") });
            db.aiUsageRecords.Add(new AiUsageRecord { Id = usageId, TenantId = "tenant-1", UserId = studentT1, Kind = AiUsageKind.Chat, Provider = "test", TotalTokens = 10 });
            db.tenantSettings.Add(new TenantSetting { Id = tenantSettingId, TenantId = "tenant-1", Key = Phase4Db.NewId("tenant-setting"), Value = "true", ValueType = SettingValueType.Boolean });
            await db.SaveChangesAsync();

            Assert.True(await db.auditLogs.AnyAsync(x => x.Id == auditId));
            Assert.True(await db.fileRecords.AnyAsync(x => x.Id == fileId));
            Assert.True(await db.aiUsageRecords.AnyAsync(x => x.Id == usageId));
            Assert.True(await db.tenantSettings.AnyAsync(x => x.Id == tenantSettingId));
        }
        finally
        {
            await CleanupAsync("tenantSettings", tenantSettingId);
            await CleanupAsync("aiUsageRecords", usageId);
            await CleanupAsync("fileRecords", fileId);
            await CleanupAsync("auditLogs", auditId);
            await CleanupAsync("featureFlags", flagId);
            await CleanupAsync("systemSettings", systemSettingId);
        }
    }

    private async Task CleanupAsync(string set, string id)
    {
        await using var db = Phase4Db.Platform(_factory);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"" + set + "\" WHERE \"Id\" = {0}", id);
    }
}
