using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;
using System.Collections.Generic;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// PLATFORM-owned catalog entry describing a subscription plan and its usage
    /// limits. Separated from per-tenant <see cref="TenantSubscription"/> records so
    /// plans, prices and limits can be administered centrally and versioned without
    /// touching tenant data. <see cref="Code"/> is globally unique.
    /// </summary>
    public class SubscriptionPlanDefinition : PlatformEntity<string>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>Coarse tier (kept for compatibility with the existing enum).</summary>
        public SubscriptionPlan Tier { get; set; } = SubscriptionPlan.Free;
        public BillingPeriod BillingPeriod { get; set; } = BillingPeriod.Monthly;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";

        // Usage limits (null = unlimited).
        public int? MaxStudents { get; set; }
        public int? MaxTeachers { get; set; }
        public int? MaxStorageMb { get; set; }
        public int? MaxAiGenerationsPerMonth { get; set; }

        public int TrialDays { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        public ICollection<TenantSubscription> Subscriptions { get; set; } = new HashSet<TenantSubscription>();
    }
}
