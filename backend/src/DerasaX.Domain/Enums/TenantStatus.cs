namespace DerasaX.Domain.Enums
{
    /// <summary>
    /// Lifecycle state of a tenant (school). Authentication and tenant resolution
    /// reject access for any tenant that is not <see cref="Active"/>.
    /// </summary>
    public enum TenantStatus
    {
        Active = 0,
        Suspended = 1,
        Archived = 2
    }
}
