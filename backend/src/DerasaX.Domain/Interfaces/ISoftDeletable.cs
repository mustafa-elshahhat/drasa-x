namespace DerasaX.Application.Services.Abstractions
{
    /// <summary>
    /// Marks an entity that participates in soft-delete. Rows with
    /// <see cref="IsDeleted"/> set are hidden by the global query filter and remain
    /// recoverable / auditable instead of being physically removed.
    /// </summary>
    public interface ISoftDeletable
    {
        bool IsDeleted { get; set; }
    }
}
