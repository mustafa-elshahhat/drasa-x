using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;

namespace DerasaX.Application.Common
{
    /// <summary>
    /// Phase 14 — the single, idempotent place that stages a gamification point transaction onto the
    /// current unit of work. Every point award (manual, competition reward, office-hour attendance,
    /// community activity) routes through here so that the same real-world event can never award twice:
    /// before adding a row we check for an existing transaction with the same per-tenant idempotency
    /// key, and the database additionally enforces a UNIQUE (TenantId, IdempotencyKey) index as the hard
    /// backstop. The staged row participates in the caller's transaction.
    /// </summary>
    public static class PointLedgerStaging
    {
        /// <summary>
        /// Stages a point transaction unless one already exists for <paramref name="idempotencyKey"/>
        /// in the current tenant. Returns the staged entity, or <c>null</c> when it was a no-op
        /// (already awarded) so callers can tell whether new points were granted.
        /// </summary>
        public static async Task<StudentPointTransaction?> StageAsync(
            IUnitOfWork uow,
            string tenantId,
            string studentId,
            int points,
            string reason,
            PointSourceType sourceType,
            string idempotencyKey,
            string? sourceId = null,
            string? gamificationRuleId = null,
            CancellationToken ct = default)
        {
            // Graceful idempotency: skip if this exact event was already recorded. The query runs
            // under the tenant filter, so it is implicitly scoped to the current tenant.
            var existing = await uow.Repository<StudentPointTransaction, string>().CountAsync(
                new CriteriaSpecification<StudentPointTransaction, string>(t => t.IdempotencyKey == idempotencyKey));
            if (existing > 0) return null;

            var tx = new StudentPointTransaction
            {
                Id = System.Guid.NewGuid().ToString(),
                TenantId = tenantId,
                StudentId = studentId,
                Points = points,
                Reason = reason,
                SourceType = sourceType,
                SourceId = sourceId,
                IdempotencyKey = idempotencyKey,
                GamificationRuleId = gamificationRuleId,
                AwardedAt = System.DateTime.UtcNow
            };
            await uow.Repository<StudentPointTransaction, string>().AddAsync(tx);
            return tx;
        }
    }
}
