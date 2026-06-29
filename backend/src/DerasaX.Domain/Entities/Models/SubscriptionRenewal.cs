using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;
using System;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// A renewal request/history entry for a <see cref="TenantSubscription"/>. Records
    /// the requested extension and its outcome so renewal history is preserved.
    /// </summary>
    public class SubscriptionRenewal : AuditableEntity<string>
    {
        public string TenantSubscriptionId { get; set; } = string.Empty;
        public TenantSubscription? TenantSubscription { get; set; }

        public RenewalStatus Status { get; set; } = RenewalStatus.Requested;
        public DateTime RequestedAt { get; set; }
        public DateTime? PreviousExpiresAt { get; set; }
        public DateTime? NewExpiresAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? Notes { get; set; }
    }
}
