namespace DerasaX.Domain.Enums
{
    /// <summary>Lifecycle state of a tenant's subscription record.</summary>
    public enum SubscriptionStatus
    {
        Trial = 0,
        Active = 1,
        PastDue = 2,
        Suspended = 3,
        Cancelled = 4,
        Expired = 5
    }

    /// <summary>Billing cadence of a subscription-plan definition.</summary>
    public enum BillingPeriod
    {
        Monthly = 0,
        Quarterly = 1,
        Annual = 2,
        Custom = 3
    }

    /// <summary>State of a subscription renewal request.</summary>
    public enum RenewalStatus
    {
        Requested = 0,
        Approved = 1,
        Rejected = 2,
        Applied = 3,
        Cancelled = 4
    }
}
