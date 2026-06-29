using System;
using DerasaX.Application.Services.Abstractions;

namespace DerasaX.Domain.Entities.Base
{
    /// <summary>
    /// Base for Phase 4 domain entities. Adds standardized audit metadata on top of
    /// <see cref="BaseEntity{Tkey}"/> (Id + soft-delete + tenant). Concurrency is
    /// handled by PostgreSQL's <c>xmin</c> system column (configured in the DbContext),
    /// so no explicit concurrency column is added here.
    /// </summary>
    public abstract class AuditableEntity<TKey> : BaseEntity<TKey>, IAuditable
    {
        /// <summary>UTC creation timestamp (stamped automatically on insert).</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>UTC last-update timestamp (stamped automatically on update).</summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>User id that created the row, when acting in an authenticated context.</summary>
        public string? CreatedBy { get; set; }

        /// <summary>User id that last updated the row, when acting in an authenticated context.</summary>
        public string? UpdatedBy { get; set; }
    }
}
