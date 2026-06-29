using DerasaX.Domain.Entities.Base;
using System;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// A periodic snapshot of a tenant's resource usage, compared against the plan
    /// limits on <see cref="SubscriptionPlanDefinition"/> for enforcement/reporting.
    /// History is preserved by keeping one row per period.
    /// </summary>
    public class TenantUsageCounter : AuditableEntity<string>
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public DateTime SnapshotAt { get; set; }

        public int StudentsCount { get; set; }
        public int TeachersCount { get; set; }
        public int StorageUsedMb { get; set; }
        public int AiGenerationsUsed { get; set; }
    }
}
