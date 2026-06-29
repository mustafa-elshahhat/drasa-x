using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// Phase 14 — a tenant-scoped, deterministic gamification rule. A rule makes the point value
    /// (and optional badge) of an automatic award configurable by a school administrator while the
    /// award logic itself stays in code and stays idempotent. When no enabled rule exists for a
    /// trigger, the awarding code falls back to a built-in default point value, so gamification is
    /// never silently disabled by missing configuration.
    /// </summary>
    public class GamificationRule : AuditableEntity<string>
    {
        /// <summary>Stable code used by the awarding code to look the rule up (e.g. "OFFICE_HOUR_ATTENDED").</summary>
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public GamificationTrigger Trigger { get; set; }

        /// <summary>Points granted when the rule fires.</summary>
        public int Points { get; set; }

        /// <summary>Optional platform badge auto-awarded alongside the points.</summary>
        public string? BadgeId { get; set; }

        public bool Enabled { get; set; } = true;
    }
}
