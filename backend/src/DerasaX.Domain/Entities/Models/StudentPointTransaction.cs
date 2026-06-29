using System;
using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// Phase 14 — an immutable gamification point ledger entry. Points are never stored as a
    /// single mutable total; a student's balance is the SUM of their transactions. Every row
    /// records WHY the points were granted (source type + source id) and carries a per-tenant
    /// unique <see cref="IdempotencyKey"/> so the same real-world event (a competition reward,
    /// an office-hour attendance, a manual award) can never be double-counted.
    /// </summary>
    public class StudentPointTransaction : AuditableEntity<string>
    {
        public string StudentId { get; set; } = string.Empty;
        public Student Student { get; set; } = default!;

        /// <summary>Signed point value (positive grants; negative corrections are allowed for admins).</summary>
        public int Points { get; set; }

        /// <summary>Human-readable reason, surfaced in the ledger UI.</summary>
        public string Reason { get; set; } = string.Empty;

        public PointSourceType SourceType { get; set; }

        /// <summary>The originating entity id (competition id, booking id, post id …), when applicable.</summary>
        public string? SourceId { get; set; }

        /// <summary>
        /// Per-tenant unique key that makes awards idempotent. For automatic awards it is derived
        /// deterministically from the source (e.g. "oh-attend:{bookingId}"); for manual awards it
        /// defaults to a fresh GUID unless the caller supplies one.
        /// </summary>
        public string IdempotencyKey { get; set; } = string.Empty;

        /// <summary>Optional link to the <see cref="GamificationRule"/> that produced an automatic award.</summary>
        public string? GamificationRuleId { get; set; }

        public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
    }
}
