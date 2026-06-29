using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;

namespace DerasaX.Application.Services.Abstractions.Storage
{
    /// <summary>
    /// Phase 16 — secure, tenant-isolated durable-file orchestration. The single safe primitive
    /// for storing/serving production binaries: validates purpose/type/size/filename, hashes,
    /// persists metadata to PostgreSQL, moves bytes through an <see cref="IFileStorageProvider"/>,
    /// enforces tenant isolation + baseline authorization, issues/validates expiring signed
    /// download tokens, soft-deletes, and writes audit records. Relationship authorization
    /// (teacher↔lesson, parent↔child) is layered by the owning workflow services, which then call
    /// the pre-authorized read path.
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>Validates and stores a new file (bytes + metadata) for the current tenant/user.</summary>
        Task<FileRecord> UploadAsync(FileUploadRequest request, CancellationToken ct = default);

        /// <summary>Tenant-isolated metadata lookup (throws NotFound across tenants).</summary>
        Task<FileRecord> GetMetadataAsync(string fileId, CancellationToken ct = default);

        /// <summary>
        /// Opens a file applying baseline authorization (tenant + owner/admin/tenant-internal
        /// visibility) and auditing the download. Used by the generic download endpoint.
        /// </summary>
        Task<FileContentResult> OpenWithBaselineAuthorizationAsync(string fileId, CancellationToken ct = default);

        /// <summary>
        /// Opens a file the caller has ALREADY relationship-authorized in a workflow service
        /// (e.g. parent linked to child, teacher assigned to lesson). Still enforces tenant +
        /// expected purpose + not-deleted, and audits the access. Never expose directly to clients.
        /// </summary>
        Task<FileContentResult> OpenPreAuthorizedAsync(string fileId, FilePurpose expectedPurpose, CancellationToken ct = default);

        /// <summary>Issues an HMAC-signed, time-limited download token for a baseline-authorized file.</summary>
        Task<SignedDownloadToken> CreateSignedDownloadTokenAsync(string fileId, TimeSpan? ttl = null, CancellationToken ct = default);

        /// <summary>Validates a signed token (HMAC + expiry + tenant match) and opens the file; audits.</summary>
        Task<FileContentResult> OpenBySignedTokenAsync(string token, CancellationToken ct = default);

        /// <summary>Soft-deletes a file (owner/admin only); future downloads are blocked. Audited.</summary>
        Task SoftDeleteAsync(string fileId, CancellationToken ct = default);
    }

    /// <summary>Request to validate + store a new durable file.</summary>
    public sealed class FileUploadRequest
    {
        public required Stream Content { get; init; }
        public required string OriginalFileName { get; init; }
        /// <summary>Client-declared content type — cross-checked against the extension; never trusted alone.</summary>
        public required string DeclaredContentType { get; init; }
        public long SizeBytes { get; init; }
        public required FilePurpose Purpose { get; init; }
        public FileVisibility? Visibility { get; init; }
        public string? RelatedEntityType { get; init; }
        public string? RelatedEntityId { get; init; }
        public DateTime? RetentionUntil { get; init; }
        public bool ConsentObtained { get; init; }
        public string? ConsentReference { get; init; }
    }

    /// <summary>An open file: metadata + a readable content stream. Dispose to release the stream.</summary>
    public sealed class FileContentResult : IDisposable
    {
        public required FileRecord Record { get; init; }
        public required Stream Content { get; init; }
        public void Dispose() => Content?.Dispose();
    }

    /// <summary>An issued signed-download token and its absolute UTC expiry.</summary>
    public sealed class SignedDownloadToken
    {
        public required string Token { get; init; }
        public required DateTime ExpiresAtUtc { get; init; }
        public required string FileId { get; init; }
    }
}
