using System;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;

namespace DerasaX.Application.Dto.StorageDto
{
    /// <summary>
    /// Phase 16 — client-safe view of a stored file. Deliberately omits the opaque
    /// <c>StorageKey</c> and any provider URL; the only way to fetch bytes is via the
    /// backend-mediated download endpoint or a short-lived signed token.
    /// </summary>
    public class StoredFileDto
    {
        public string Id { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public FilePurpose Purpose { get; set; }
        public FileVisibility Visibility { get; set; }
        public FileScanStatus ScanStatus { get; set; }
        public string? ChecksumSha256 { get; set; }
        public DateTime CreatedAt { get; set; }
        /// <summary>Relative, backend-mediated download path (never a raw storage URL).</summary>
        public string DownloadUrl { get; set; } = string.Empty;

        public static StoredFileDto From(FileRecord r) => new()
        {
            Id = r.Id,
            FileName = r.FileName,
            ContentType = r.ContentType,
            SizeBytes = r.SizeBytes,
            Purpose = r.Purpose,
            Visibility = r.Visibility,
            ScanStatus = r.ScanStatus,
            ChecksumSha256 = r.ChecksumSha256,
            CreatedAt = r.CreatedAt,
            DownloadUrl = $"/api/v1/files/{r.Id}/download"
        };
    }

    /// <summary>A short-lived signed download token + the relative URL to redeem it.</summary>
    public class SignedDownloadDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        /// <summary>Relative URL that streams the file when given this token (no auth header needed).</summary>
        public string DownloadUrl { get; set; } = string.Empty;
    }
}
