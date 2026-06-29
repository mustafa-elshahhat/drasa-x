using System;
using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// Durable file metadata — the single system-of-record row for every production binary
    /// (Phase 16). Binaries live in a storage provider keyed by <see cref="StorageKey"/>; this
    /// row in PostgreSQL owns identity, tenant ownership, purpose, validation result, retention,
    /// scan state, and soft-delete. The opaque <see cref="StorageKey"/> is never a public URL.
    /// </summary>
    public class FileRecord : AuditableEntity<string>
    {
        // --- Phase 5 fields (retained for backward compatibility) ---
        /// <summary>Original (client-supplied) file name, sanitized for safe display.</summary>
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        /// <summary>Opaque, tenant-scoped storage key — NEVER an insecure permanent public URL.</summary>
        public string StorageKey { get; set; } = string.Empty;
        public string? ChecksumSha256 { get; set; }
        public FileRecordType Type { get; set; } = FileRecordType.Other;
        /// <summary>Owning user id (uploader) where applicable.</summary>
        public string? UploadedByUserId { get; set; }
        public ApplicationUser? UploadedByUser { get; set; }

        // --- Phase 16 durable-storage fields ---
        /// <summary>Sanitized leaf name actually persisted by the provider (no path, no traversal).</summary>
        public string SafeStoredFileName { get; set; } = string.Empty;
        /// <summary>Business purpose — drives validation, sensitivity and authorization.</summary>
        public FilePurpose Purpose { get; set; } = FilePurpose.Other;
        /// <summary>Provider that owns the bytes (e.g. "Local", "S3").</summary>
        public string StorageProvider { get; set; } = string.Empty;
        /// <summary>Bucket / container when the provider uses one (null for the local provider).</summary>
        public string? StorageBucket { get; set; }
        /// <summary>Sensitivity classification controlling baseline read authorization.</summary>
        public FileVisibility Visibility { get; set; } = FileVisibility.TenantInternal;
        /// <summary>Owning workflow entity type (e.g. "Lesson", "ParentRequest").</summary>
        public string? RelatedEntityType { get; set; }
        public string? RelatedEntityId { get; set; }
        /// <summary>Optional retention deadline; eligible for purge/cleanup once passed.</summary>
        public DateTime? RetentionUntil { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByUserId { get; set; }
        /// <summary>Honest virus-scan state — never set to Clean without a real scan.</summary>
        public FileScanStatus ScanStatus { get; set; } = FileScanStatus.NotScanned;
        /// <summary>True only when explicit, recorded consent was captured (CV enrollment assets).</summary>
        public bool ConsentObtained { get; set; }
        /// <summary>Free-text reference to the consent record / basis (no PII).</summary>
        public string? ConsentReference { get; set; }
    }
}
