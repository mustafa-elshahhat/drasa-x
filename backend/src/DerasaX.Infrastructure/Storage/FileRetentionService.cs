using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Application.Services.Abstractions.Storage;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Infrastructure.DbHelper.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DerasaX.Infrastructure.Storage
{
    /// <summary>
    /// Phase 19 — cross-tenant file-retention/purge maintenance (see
    /// <see cref="IFileRetentionService"/>). Operates directly on the DbContext with the
    /// per-tenant query filter bypassed (a system maintenance job is not a request). It never
    /// touches non-eligible rows and records every action to the audit trail + job heartbeat.
    /// </summary>
    public sealed class FileRetentionService : IFileRetentionService
    {
        public const string JobName = "file-retention";
        private const string SystemActor = "system:retention";

        private readonly DerasaXDbContext _db;
        private readonly IFileStorageProvider _provider;
        private readonly IBackgroundJobHealth _jobHealth;
        private readonly RetentionOptions _options;
        private readonly ILogger<FileRetentionService> _logger;

        public FileRetentionService(
            DerasaXDbContext db,
            IFileStorageProvider provider,
            IBackgroundJobHealth jobHealth,
            IOptions<FileStorageSettings> settings,
            ILogger<FileRetentionService> logger)
        {
            _db = db;
            _provider = provider;
            _jobHealth = jobHealth;
            _options = settings.Value.Retention;
            _logger = logger;
        }

        public async Task<RetentionRunResult> RunOnceAsync(CancellationToken ct = default)
        {
            var result = new RetentionRunResult();
            try
            {
                var now = DateTime.UtcNow;

                // 1) Soft-delete file records whose retention deadline has passed (reads blocked at once).
                var expired = await _db.fileRecords.IgnoreQueryFilters()
                    .Where(f => !f.IsDeleted && f.RetentionUntil != null && f.RetentionUntil < now)
                    .ToListAsync(ct);
                foreach (var rec in expired)
                {
                    rec.IsDeleted = true;
                    rec.DeletedAt = now;
                    rec.DeletedByUserId = SystemActor;
                    AddAudit(AuditActionType.Delete, rec, "retention-soft-delete");
                }
                result.SoftDeletedExpired = expired.Count;

                // 2) Purge CV enrollment assets past their retention deadline (consent lifecycle).
                var expiredAssets = await _db.studentFaceEnrollments.IgnoreQueryFilters()
                    .Where(e => e.FileRecordId != null && e.AssetRetentionUntil != null && e.AssetRetentionUntil < now)
                    .ToListAsync(ct);
                foreach (var enr in expiredAssets)
                {
                    await SoftDeleteLinkedAssetAsync(enr, SystemActor, "retention-enrollment-asset-purge", now, ct);
                    result.EnrollmentAssetsPurged++;
                }

                // 3) Optional hard-purge: permanently remove bytes + rows soft-deleted past the grace window.
                if (_options.HardPurgeEnabled)
                {
                    var cutoff = now.AddDays(-Math.Max(0, _options.HardPurgeGraceDays));
                    var purgeable = await _db.fileRecords.IgnoreQueryFilters()
                        .Where(f => f.IsDeleted && f.DeletedAt != null && f.DeletedAt < cutoff)
                        .ToListAsync(ct);
                    foreach (var rec in purgeable)
                    {
                        try { await _provider.DeleteAsync(rec.StorageKey, ct); }
                        catch (Exception ex) { _logger.LogWarning(ex, "Hard-purge byte delete failed for {FileId} (continuing).", rec.Id); }
                        AddAudit(AuditActionType.Delete, rec, "retention-hard-purge");
                        _db.fileRecords.Remove(rec);
                    }
                    result.HardPurged = purgeable.Count;
                }

                if (result.TotalAffected > 0)
                    await _db.SaveChangesAsync(ct);

                _jobHealth.RecordRun(JobName, success: true,
                    note: $"softDeleted={result.SoftDeletedExpired}, assetsPurged={result.EnrollmentAssetsPurged}, hardPurged={result.HardPurged}",
                    affected: result.TotalAffected);
                _logger.LogInformation(
                    "file-retention pass complete: softDeleted={SoftDeleted} assetsPurged={AssetsPurged} hardPurged={HardPurged}",
                    result.SoftDeletedExpired, result.EnrollmentAssetsPurged, result.HardPurged);
                return result;
            }
            catch (Exception ex)
            {
                _jobHealth.RecordRun(JobName, success: false, note: ex.GetType().Name, affected: 0);
                _logger.LogError(ex, "file-retention pass failed.");
                throw;
            }
        }

        public async Task<bool> RevokeEnrollmentAssetAsync(string enrollmentId, string? actorUserId, CancellationToken ct = default)
        {
            var enr = await _db.studentFaceEnrollments.IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == enrollmentId, ct);
            if (enr is null || string.IsNullOrEmpty(enr.FileRecordId))
                return false;

            await SoftDeleteLinkedAssetAsync(enr, actorUserId ?? SystemActor, "cv-consent-revoke", DateTime.UtcNow, ct);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        // Soft-deletes the file record linked to an enrollment and clears the enrollment's
        // asset/consent fields (idempotent if the record is already gone/deleted).
        private async Task SoftDeleteLinkedAssetAsync(StudentFaceEnrollment enr, string actor, string action, DateTime now, CancellationToken ct)
        {
            var rec = await _db.fileRecords.IgnoreQueryFilters()
                .FirstOrDefaultAsync(f => f.Id == enr.FileRecordId, ct);
            if (rec is not null && !rec.IsDeleted)
            {
                rec.IsDeleted = true;
                rec.DeletedAt = now;
                rec.DeletedByUserId = actor;
                AddAudit(AuditActionType.Delete, rec, action);
            }
            enr.FileRecordId = null;
            enr.ConsentObtained = false;
            enr.AssetRetentionUntil = null;
        }

        private void AddAudit(AuditActionType action, FileRecord rec, string reason)
        {
            // System-initiated maintenance: ActorUserId is NULL (a DB trigger enforces that any
            // non-null actor must be a real user in the row's tenant). The acting "system:retention"
            // identity + reason are recorded in the metadata instead.
            _db.auditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = rec.TenantId,
                ActorUserId = null,
                Action = action,
                EntityType = nameof(FileRecord),
                EntityId = rec.Id,
                OccurredAt = DateTime.UtcNow,
                MetadataJson = $"{{\"actor\":\"{SystemActor}\",\"reason\":\"{reason}\",\"purpose\":\"{rec.Purpose}\",\"visibility\":\"{rec.Visibility}\"}}"
            });
        }
    }
}
