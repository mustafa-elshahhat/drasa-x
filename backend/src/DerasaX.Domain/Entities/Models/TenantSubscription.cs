using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;
using System;
using System.Collections.Generic;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// A tenant's assignment to a subscription plan. Each assignment is one row, so the
    /// table doubles as plan-assignment history; the current subscription is the latest
    /// non-terminal row for the tenant. Carries trial, start/expiry, suspension and
    /// reactivation data.
    /// </summary>
    public class TenantSubscription : AuditableEntity<string>
    {
        public string PlanDefinitionId { get; set; } = string.Empty;
        public SubscriptionPlanDefinition? PlanDefinition { get; set; }

        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trial;

        public DateTime StartsAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public bool IsTrial { get; set; }
        public DateTime? TrialEndsAt { get; set; }

        public bool AutoRenew { get; set; } = true;

        public DateTime? SuspendedAt { get; set; }
        public DateTime? ReactivatedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }

        public ICollection<SubscriptionRenewal> Renewals { get; set; } = new HashSet<SubscriptionRenewal>();
    }
}
