using System;
using System.Linq;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions.Storage;
using DerasaX.Application.Services.Operations;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Infrastructure.DbHelper.Context;
using DerasaX.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 19 — file-retention/purge + CV consent-revoke purge, exercised against the live PostgreSQL
/// through the real <see cref="FileRetentionService"/>. Proves: purge-eligible records are soft-deleted,
/// non-eligible (future/no-retention) records are preserved, the sweep is cross-tenant (tenant
/// isolation: tenant A's expiry never deletes tenant B's non-eligible row), audit rows are written,
/// opt-in hard-purge removes only past-grace soft-deleted rows, and consent-revoke purges the linked
/// asset. Fully self-cleaning (deletes only its own rows).
/// </summary>
public class Phase19RetentionTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public Phase19RetentionTests(IntegrationFactory factory) => _factory = factory;

    private FileRetentionService NewService(DerasaXDbContext db, bool hardPurge = false, int graceDays = 30)
    {
        var provider = _factory.Services.GetRequiredService<IFileStorageProvider>();
        var settings = Options.Create(new FileStorageSettings
        {
            Retention = new RetentionOptions { Enabled = true, HardPurgeEnabled = hardPurge, HardPurgeGraceDays = graceDays }
        });
        return new FileRetentionService(db, provider, new BackgroundJobHealth(), settings,
            NullLogger<FileRetentionService>.Instance);
    }

    private static FileRecord Rec(string tenantId, DateTime? retentionUntil, bool deleted = false, DateTime? deletedAt = null,
        FilePurpose purpose = FilePurpose.Other, bool consent = false)
    {
        var id = $"ph19ret-{Guid.NewGuid():N}";
        return new FileRecord
        {
            Id = id,
            TenantId = tenantId,
            FileName = "ph19.pdf",
            ContentType = "application/pdf",
            SizeBytes = 64,
            StorageKey = $"{tenantId}/ph19/{id}.pdf",
            StorageProvider = "Local",
            Purpose = purpose,
            Visibility = FileVisibility.Private,
            RetentionUntil = retentionUntil,
            IsDeleted = deleted,
            DeletedAt = deletedAt,
            ConsentObtained = consent
        };
    }

    private async Task DeleteRowsAsync(params string[] fileIds)
    {
        await using var db = Phase4Db.Platform(_factory);
        foreach (var id in fileIds.Where(i => !string.IsNullOrEmpty(i)))
        {
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"auditLogs\" WHERE \"EntityId\" = {0}", id);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"fileRecords\" WHERE \"Id\" = {0}", id);
        }
    }

    [Fact]
    public async Task Retention_soft_deletes_expired_preserves_others_across_tenants_and_audits()
    {
        var expired1 = Rec("tenant-1", DateTime.UtcNow.AddDays(-1));
        var future1 = Rec("tenant-1", DateTime.UtcNow.AddDays(30));
        var none1 = Rec("tenant-1", null);
        var expired2 = Rec("tenant-2", DateTime.UtcNow.AddDays(-2)); // different tenant, also eligible

        await using (var seed = Phase4Db.Platform(_factory))
        {
            seed.fileRecords.AddRange(expired1, future1, none1, expired2);
            await seed.SaveChangesAsync();
        }

        try
        {
            await using (var svcDb = Phase4Db.Platform(_factory))
            {
                var result = await NewService(svcDb).RunOnceAsync();
                Assert.True(result.SoftDeletedExpired >= 2, "both expired records (across tenants) must be soft-deleted");
            }

            await using var verify = Phase4Db.Platform(_factory);
            async Task<FileRecord?> Get(string id) =>
                await verify.fileRecords.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == id);

            Assert.True((await Get(expired1.Id))!.IsDeleted, "expired tenant-1 file must be soft-deleted");
            Assert.True((await Get(expired2.Id))!.IsDeleted, "expired tenant-2 file must be soft-deleted (cross-tenant sweep)");
            Assert.False((await Get(future1.Id))!.IsDeleted, "future-retention file must be preserved");
            Assert.False((await Get(none1.Id))!.IsDeleted, "no-retention file must be preserved");

            // Soft-delete is audited.
            var audited = await verify.auditLogs.IgnoreQueryFilters()
                .AnyAsync(a => a.EntityId == expired1.Id && a.Action == AuditActionType.Delete);
            Assert.True(audited, "soft-delete must write an audit row");
        }
        finally
        {
            await DeleteRowsAsync(expired1.Id, future1.Id, none1.Id, expired2.Id);
        }
    }

    [Fact]
    public async Task Hard_purge_removes_only_rows_soft_deleted_past_grace_window()
    {
        var oldDeleted = Rec("tenant-1", null, deleted: true, deletedAt: DateTime.UtcNow.AddDays(-60));
        var recentDeleted = Rec("tenant-1", null, deleted: true, deletedAt: DateTime.UtcNow.AddDays(-1));

        await using (var seed = Phase4Db.Platform(_factory))
        {
            seed.fileRecords.AddRange(oldDeleted, recentDeleted);
            await seed.SaveChangesAsync();
        }

        try
        {
            await using (var svcDb = Phase4Db.Platform(_factory))
            {
                var result = await NewService(svcDb, hardPurge: true, graceDays: 30).RunOnceAsync();
                Assert.True(result.HardPurged >= 1);
            }

            await using var verify = Phase4Db.Platform(_factory);
            var oldStill = await verify.fileRecords.IgnoreQueryFilters().AnyAsync(f => f.Id == oldDeleted.Id);
            var recentStill = await verify.fileRecords.IgnoreQueryFilters().AnyAsync(f => f.Id == recentDeleted.Id);
            Assert.False(oldStill, "row soft-deleted past the grace window must be hard-purged");
            Assert.True(recentStill, "recently soft-deleted row must NOT be hard-purged yet");
        }
        finally
        {
            await DeleteRowsAsync(oldDeleted.Id, recentDeleted.Id);
        }
    }

    [Fact]
    public async Task Consent_revoke_purges_the_linked_enrollment_asset()
    {
        string? studentId;
        await using (var lookup = Phase4Db.Platform(_factory))
        {
            studentId = await lookup.Set<Student>().IgnoreQueryFilters()
                .Where(s => s.TenantId == "tenant-1").Select(s => s.Id).FirstOrDefaultAsync();
        }
        Assert.False(string.IsNullOrEmpty(studentId), "expected a seeded student in tenant-1");

        var asset = Rec("tenant-1", DateTime.UtcNow.AddYears(1), purpose: FilePurpose.CvEnrollmentAsset, consent: true);
        asset.Visibility = FileVisibility.Sensitive;
        var enrollmentId = $"ph19enr-{Guid.NewGuid():N}";

        await using (var seed = Phase4Db.Platform(_factory))
        {
            seed.fileRecords.Add(asset);
            seed.studentFaceEnrollments.Add(new StudentFaceEnrollment
            {
                Id = enrollmentId, TenantId = "tenant-1", StudentId = studentId!,
                ExternalLabelId = "ph19-label", IsActive = true, Source = "manual", EnrolledAt = DateTime.UtcNow,
                ConsentObtained = true, ConsentReference = "ph19-consent", AssetRetentionUntil = DateTime.UtcNow.AddYears(1),
                FileRecordId = asset.Id
            });
            await seed.SaveChangesAsync();
        }

        try
        {
            await using (var svcDb = Phase4Db.Platform(_factory))
            {
                var purged = await NewService(svcDb).RevokeEnrollmentAssetAsync(enrollmentId, "ph19-admin");
                Assert.True(purged, "revoke must report the asset purged");
            }

            await using var verify = Phase4Db.Platform(_factory);
            var rec = await verify.fileRecords.IgnoreQueryFilters().FirstAsync(f => f.Id == asset.Id);
            var enr = await verify.studentFaceEnrollments.IgnoreQueryFilters().FirstAsync(e => e.Id == enrollmentId);
            Assert.True(rec.IsDeleted, "linked file record must be soft-deleted on consent revoke");
            Assert.Null(enr.FileRecordId);
            Assert.False(enr.ConsentObtained);
        }
        finally
        {
            await using var db = Phase4Db.Platform(_factory);
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"studentFaceEnrollments\" WHERE \"Id\" = {0}", enrollmentId);
            await DeleteRowsAsync(asset.Id);
        }
    }

    [Fact]
    public void Retention_local_defaults_are_safe()
    {
        // PR-3 (SEC-05) — the default/local posture must NEVER run the destructive sweep or hard-purge.
        // Staging/prod opt in explicitly via FileStorage:Retention:{Enabled,HardPurgeEnabled}=true in config.
        var opts = new RetentionOptions();
        Assert.False(opts.Enabled, "background retention sweep must be OFF by default (local-safe)");
        Assert.False(opts.HardPurgeEnabled, "hard purge must be OFF by default (soft-delete-only)");
        Assert.True(opts.HardPurgeGraceDays >= 1, "a non-trivial grace window must be the default");
    }
}
