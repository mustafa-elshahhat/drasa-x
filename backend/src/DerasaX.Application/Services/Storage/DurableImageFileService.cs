using System;
using System.IO;
using System.Threading.Tasks;
using DerasaX.Application.Services.Abstractions.Storage;
using DerasaX.Application.Services.Image.FileServices;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DerasaX.Application.Services.Storage
{
    /// <summary>
    /// Phase 19 — legacy image migration. Implements the legacy <see cref="IFileService"/> contract
    /// (used by Subject images) but routes NEW uploads through the durable Phase 16
    /// <see cref="IFileStorageService"/> (FileRecord + provider + scan + audit + retention) instead of
    /// raw <c>wwwroot/Images</c>. The returned value is the durable FileRecord id (no extension), which
    /// <c>GenericPictureUrlResolver</c> turns into a backend-mediated download URL. Deletes route by
    /// shape: a durable id is soft-deleted via the storage service; a legacy "{guid}.ext" filename is
    /// deleted from the old wwwroot location (best-effort) so pre-existing data still works.
    /// </summary>
    public sealed class DurableImageFileService : IFileService
    {
        private readonly IFileStorageService _storage;
        private readonly FileService _legacy; // legacy wwwroot provider, for backward-compatible deletes
        private readonly ILogger<DurableImageFileService> _logger;

        public DurableImageFileService(IFileStorageService storage, FileService legacy, ILogger<DurableImageFileService> logger)
        {
            _storage = storage;
            _legacy = legacy;
            _logger = logger;
        }

        public async Task<string> UploadImageAsync(IFormFile imageFile, string? folderName)
        {
            if (imageFile is null || imageFile.Length == 0)
                throw new ImageValidationException("Image file cannot be empty.");

            await using var stream = imageFile.OpenReadStream();
            var record = await _storage.UploadAsync(new FileUploadRequest
            {
                Content = stream,
                OriginalFileName = imageFile.FileName,
                DeclaredContentType = ResolveContentType(imageFile),
                SizeBytes = imageFile.Length,
                // Tenant-internal image purpose (<=5MB, jpg/jpeg/png/gif/webp): fits subject/profile images.
                Purpose = FilePurpose.ProfileImage,
                RelatedEntityType = folderName
            });

            // The durable FileRecord id (a GUID, no extension) is stored on the owning entity.
            return record.Id;
        }

        public void DeleteImage(string imageUrl, string? folderName)
        {
            if (string.IsNullOrEmpty(imageUrl))
                throw new ImageValidationException("Image URL cannot be null or empty.");

            // Legacy on-disk filename ("{guid}.png") -> delegate to the old wwwroot provider.
            if (Path.HasExtension(imageUrl))
            {
                try { _legacy.DeleteImage(imageUrl, folderName); }
                catch (Exception ex) { _logger.LogWarning(ex, "Legacy image delete skipped for {Image}.", imageUrl); }
                return;
            }

            // Durable FileRecord id -> soft-delete via the storage service. Best-effort cleanup:
            // image deletion must never fail the owning entity's update/delete (matches prior behaviour).
            try { _storage.SoftDeleteAsync(imageUrl).GetAwaiter().GetResult(); }
            catch (Exception ex) { _logger.LogWarning(ex, "Durable image soft-delete skipped for {FileId}.", imageUrl); }
        }

        // IFormFile.ContentType is set by the browser for real uploads; fall back to the extension
        // so the durable policy's content-type cross-check still passes when it is absent.
        private static string ResolveContentType(IFormFile file)
        {
            var ct = (file.ContentType ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(ct) && ct != "application/octet-stream") return ct;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }
}
