using System.Threading;
using System.Threading.Tasks;

namespace DerasaX.Application.Services.Abstractions.Storage
{
    /// <summary>
    /// Phase 19 — durable-file retention/purge. Runs as a maintenance task (cross-tenant,
    /// bypassing the per-tenant query filter), NOT in a user request context:
    ///  - soft-deletes file records whose <c>RetentionUntil</c> has passed (reads blocked immediately),
    ///  - purges CV enrollment assets whose <c>AssetRetentionUntil</c> has passed (consent lifecycle),
    ///  - optionally hard-purges bytes + rows of files soft-deleted past a configured grace window.
    /// Every action is recorded to the audit trail and the background-job heartbeat. Honest by
    /// construction: it never deletes a non-eligible record and preserves tenant isolation.
    /// </summary>
    public interface IFileRetentionService
    {
        /// <summary>Runs one full retention pass and returns the affected counts.</summary>
        Task<RetentionRunResult> RunOnceAsync(CancellationToken ct = default);

        /// <summary>
        /// Consent-revoke purge for a single CV enrollment asset: soft-deletes the linked file
        /// record and clears the enrollment's asset/consent fields. Returns true if an asset was purged.
        /// </summary>
        Task<bool> RevokeEnrollmentAssetAsync(string enrollmentId, string? actorUserId, CancellationToken ct = default);
    }

    /// <summary>Affected-row counts from one retention pass (no PII).</summary>
    public sealed class RetentionRunResult
    {
        public int SoftDeletedExpired { get; set; }
        public int EnrollmentAssetsPurged { get; set; }
        public int HardPurged { get; set; }
        public int TotalAffected => SoftDeletedExpired + EnrollmentAssetsPurged + HardPurged;
    }
}
