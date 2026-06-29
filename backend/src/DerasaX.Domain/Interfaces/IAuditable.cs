using System;

namespace DerasaX.Application.Services.Abstractions
{
    /// <summary>
    /// Entities that carry creation/modification audit metadata. Timestamps are
    /// stamped automatically (UTC) by the DbContext; the *-By fields capture the
    /// acting user id from the trusted request context when available.
    /// </summary>
    public interface IAuditable
    {
        DateTime CreatedAt { get; set; }
        DateTime? UpdatedAt { get; set; }
        string? CreatedBy { get; set; }
        string? UpdatedBy { get; set; }
    }
}
