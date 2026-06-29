using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// Phase 13 — a per-user, per-category notification preference. Absence of a row means the
    /// category uses its default (in-app on, e-mail off). Mandatory categories
    /// (<see cref="DerasaX.Domain.Enums.NotificationCategory.Warning"/>) are never suppressed regardless
    /// of any stored row (enforced in routing and rejected at the API).
    /// </summary>
    public class NotificationPreference : BaseEntity<string>
    {
        [ForeignKey("User")]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public NotificationCategory Category { get; set; }

        /// <summary>Whether persisted/in-app notifications for this category reach the user's inbox.</summary>
        public bool InAppEnabled { get; set; } = true;

        /// <summary>Whether the user wants e-mail for this category. Recorded even though the e-mail
        /// channel is not configured in this environment (delivery is reported honestly, never faked).</summary>
        public bool EmailEnabled { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
