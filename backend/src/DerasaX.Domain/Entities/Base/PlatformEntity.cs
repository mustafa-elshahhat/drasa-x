using System;
using DerasaX.Application.Services.Abstractions;

namespace DerasaX.Domain.Entities.Base
{
    /// <summary>
    /// Base for PLATFORM-owned entities (no tenant). These are global records
    /// administered by the platform itself — e.g. subscription-plan definitions,
    /// global feature flags / system settings. They are soft-deletable and audited
    /// but intentionally do NOT implement <see cref="IMustHaveTenant"/>, so the
    /// tenant query filter and tenant FK are not applied to them.
    /// </summary>
    public abstract class PlatformEntity<TKey> : ISoftDeletable, IAuditable
    {
        public TKey Id { get; set; } = default!;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
