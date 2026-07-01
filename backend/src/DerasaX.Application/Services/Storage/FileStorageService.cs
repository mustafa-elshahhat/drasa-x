using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Application.Services.Abstractions.Storage;
using DerasaX.Application.Services.Operations;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace DerasaX.Application.Services.Storage
{
    /// <summary>
    /// Phase 16 — secure, tenant-isolated durable-file orchestration (see
    /// <see cref="IFileStorageService"/>). Owns validation, hashing, metadata persistence,
    /// provider byte movement, baseline authorization, expiring signed tokens, soft-delete and
    /// audit. Honest by construction: scanning is recorded as NotScanned, provider failures
    /// surface as <see cref="StorageUnavailableException"/>, signed tokens are really verified.
    /// </summary>
    public class FileStorageService : OperationsServiceBase, IFileStorageService
    {
        private readonly IFileStorageProvider _provider;
        private readonly IFileRecordLookup _lookup;
        private readonly IFileScanner _scanner;
        private readonly FileStorageSettings _settings;
        private readonly byte[] _signingKey;
        private readonly IPlanLimitEnforcer _limits;

        public FileStorageService(
            IUnitOfWork uow, ITenantContext tenant, IAuditWriter audit,
            IFileStorageProvider provider, IFileRecordLookup lookup, IFileScanner scanner,
            IOptions<FileStorageSettings> settings, IConfiguration configuration, IPlanLimitEnforcer limits)
            : base(uow, tenant, audit)
        {
            _provider = provider;
            _lookup = lookup;
            _scanner = scanner;
            _limits = limits;
            _settings = settings.Value;
            var key = !string.IsNullOrWhiteSpace(_settings.SigningKey) ? _settings.SigningKey : configuration["SecretKey"];
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("File storage signing key is not configured (FileStorage:SigningKey or SecretKey).");
            _signingKey = Encoding.UTF8.GetBytes(key);
        }

        public async Task<FileRecord> UploadAsync(FileUploadRequest request, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var caller = RequireUser();

            var safeName = StorageSafety.SanitizeFileName(request.OriginalFileName);
            var ext = StorageSafety.Extension(safeName);
            var rule = FilePurposePolicy.For(request.Purpose);

            if (string.IsNullOrWhiteSpace(ext) || !rule.Extensions.Contains(ext))
                throw new BadRequestException($"File type '{ext}' is not allowed for {request.Purpose}.");

            var declaredCt = (request.DeclaredContentType ?? string.Empty).Split(';')[0].Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(declaredCt) || !rule.ContentTypes.Contains(declaredCt))
                throw new BadRequestException($"Content type '{declaredCt}' is not allowed for {request.Purpose}.");

            if (request.SizeBytes <= 0)
                throw new BadRequestException("File is empty.");
            if (request.SizeBytes > rule.MaxBytes)
                throw new BadRequestException($"File exceeds the maximum size of {rule.MaxBytes / (1024 * 1024)} MB for {request.Purpose}.");

            if (rule.RequiresConsent && !request.ConsentObtained)
                throw new BadRequestException($"Explicit consent is required to store a {request.Purpose}.");

            await _limits.EnsureCanUploadBytesAsync(tenantId, request.SizeBytes, ct);

            var visibility = request.Visibility ?? rule.DefaultVisibility;
            var storageKey = StorageSafety.BuildStorageKey(tenantId, request.Purpose, ext);
            StorageSafety.EnsureSafeStorageKey(storageKey);

            // Stream the bytes through the provider while computing SHA-256 + the authoritative size.
            using var hashing = new HashingReadStream(request.Content);
            await _provider.SaveAsync(storageKey, hashing, declaredCt, ct);
            var sha = hashing.Finish();
            var actualSize = hashing.BytesRead;

            if (actualSize > rule.MaxBytes)
            {
                await SafeDeleteBytesAsync(storageKey);
                throw new BadRequestException($"File exceeds the maximum size of {rule.MaxBytes / (1024 * 1024)} MB for {request.Purpose}.");
            }

            // Phase 18 — malware scan. Infected content is rejected and its bytes removed;
            // when no scanner is active the status is recorded honestly (NotScanned), never faked Clean.
            var scanStatus = await ScanUploadedAsync(storageKey, safeName, declaredCt, ct);

            var record = new FileRecord
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                FileName = safeName,
                SafeStoredFileName = System.IO.Path.GetFileName(storageKey),
                ContentType = declaredCt,
                SizeBytes = actualSize,
                StorageKey = storageKey,
                ChecksumSha256 = sha,
                Purpose = request.Purpose,
                Type = FilePurposePolicy.ToLegacyType(request.Purpose),
                StorageProvider = _provider.Name,
                StorageBucket = _settings.Provider.Equals("S3", StringComparison.OrdinalIgnoreCase) ? _settings.S3.Bucket : null,
                Visibility = visibility,
                RelatedEntityType = request.RelatedEntityType,
                RelatedEntityId = request.RelatedEntityId,
                RetentionUntil = request.RetentionUntil,
                ScanStatus = scanStatus, // honest: NotScanned when no scanner; Clean/Infected/Unavailable when enabled
                ConsentObtained = request.ConsentObtained,
                ConsentReference = request.ConsentReference,
                UploadedByUserId = caller
            };

            try
            {
                await UnitOfWork.Repository<FileRecord, string>().AddAsync(record);
                await Audit.StageAsync(AuditActionType.Create, nameof(FileRecord), record.Id,
                    Meta("upload", record), ct);
                await UnitOfWork.SaveChangesAsync(ct);
            }
            catch
            {
                // Metadata commit failed after bytes were written — avoid an orphaned object.
                await SafeDeleteBytesAsync(storageKey);
                throw;
            }

            return record;
        }

        public async Task<FileRecord> GetMetadataAsync(string fileId, CancellationToken ct = default)
        {
            RequireTenant();
            return await LoadTenantScopedAsync(fileId);
        }

        public async Task<FileContentResult> OpenWithBaselineAuthorizationAsync(string fileId, CancellationToken ct = default)
        {
            RequireTenant();
            var record = await LoadTenantScopedAsync(fileId);
            EnsureBaselineRead(record);
            var content = await OpenBytesAsync(record, ct);
            await AuditDownloadAsync(record, "download", ct);
            return new FileContentResult { Record = record, Content = content };
        }

        public async Task<FileContentResult> OpenPreAuthorizedAsync(string fileId, FilePurpose expectedPurpose, CancellationToken ct = default)
        {
            RequireTenant();
            var record = await LoadTenantScopedAsync(fileId);
            if (record.Purpose != expectedPurpose)
                throw new ForbiddenException("File does not match the expected purpose for this operation.");
            var content = await OpenBytesAsync(record, ct);
            await AuditDownloadAsync(record, "download-workflow", ct);
            return new FileContentResult { Record = record, Content = content };
        }

        public async Task<SignedDownloadToken> CreateSignedDownloadTokenAsync(string fileId, TimeSpan? ttl = null, CancellationToken ct = default)
        {
            RequireTenant();
            var record = await LoadTenantScopedAsync(fileId);
            EnsureBaselineRead(record);

            var lifetime = ttl ?? TimeSpan.FromSeconds(_settings.SignedUrlTtlSeconds <= 0 ? 300 : _settings.SignedUrlTtlSeconds);
            var expires = DateTime.UtcNow.Add(lifetime);
            var token = Sign(record.Id, record.TenantId!, expires);

            await Audit.StageAsync(AuditActionType.Export, nameof(FileRecord), record.Id,
                Meta("signed-token-issued", record), ct);
            await UnitOfWork.SaveChangesAsync(ct);

            return new SignedDownloadToken { Token = token, ExpiresAtUtc = expires, FileId = record.Id };
        }

        public async Task<FileContentResult> OpenBySignedTokenAsync(string token, CancellationToken ct = default)
        {
            if (!TryVerify(token, out var fileId, out var tenantId))
                throw new BadRequestException("Invalid or expired download token.");

            // Anonymous path: bypass the tenant filter (the HMAC proves issuance) but re-check tenant.
            var record = await _lookup.FindGlobalAsync(fileId, ct);
            if (record is null || record.IsDeleted || !string.Equals(record.TenantId, tenantId, StringComparison.Ordinal))
                throw new NotFoundException("File not found.");

            var content = await OpenBytesAsync(record, ct);
            await Audit.StageAsync(AuditActionType.Export, nameof(FileRecord), record.Id,
                Meta("download-signed-token", record), ct, tenantOverride: record.TenantId);
            await UnitOfWork.SaveChangesAsync(ct);
            return new FileContentResult { Record = record, Content = content };
        }

        public async Task SoftDeleteAsync(string fileId, CancellationToken ct = default)
        {
            RequireTenant();
            var caller = RequireUser();
            var record = await LoadTenantScopedAsync(fileId);

            var isOwner = string.Equals(record.UploadedByUserId, caller, StringComparison.Ordinal);
            if (!isOwner && !IsSchoolAdmin && !IsSystemAdmin)
                throw new ForbiddenException("You are not allowed to delete this file.");

            record.IsDeleted = true;
            record.DeletedAt = DateTime.UtcNow;
            record.DeletedByUserId = caller;
            UnitOfWork.Repository<FileRecord, string>().Update(record);
            await Audit.StageAsync(AuditActionType.Delete, nameof(FileRecord), record.Id, Meta("soft-delete", record), ct);
            await UnitOfWork.SaveChangesAsync(ct);
        }

        // ---- helpers ----

        private async Task<FileRecord> LoadTenantScopedAsync(string fileId) =>
            await UnitOfWork.Repository<FileRecord, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<FileRecord, string>(f => f.Id == fileId))
            ?? throw new NotFoundException("File not found.");

        private void EnsureBaselineRead(FileRecord record)
        {
            var caller = Tenant.UserId;
            if (string.Equals(record.UploadedByUserId, caller, StringComparison.Ordinal)) return;
            if (IsSchoolAdmin || IsSystemAdmin) return;
            if (record.Visibility == FileVisibility.TenantInternal) return;
            throw new ForbiddenException("You are not allowed to access this file.");
        }

        private async Task<System.IO.Stream> OpenBytesAsync(FileRecord record, CancellationToken ct)
        {
            StorageSafety.EnsureSafeStorageKey(record.StorageKey);
            return await _provider.OpenReadAsync(record.StorageKey, ct);
        }

        private async Task AuditDownloadAsync(FileRecord record, string action, CancellationToken ct)
        {
            await Audit.StageAsync(AuditActionType.Export, nameof(FileRecord), record.Id, Meta(action, record), ct);
            await UnitOfWork.SaveChangesAsync(ct);
        }

        private async Task SafeDeleteBytesAsync(string storageKey)
        {
            try { await _provider.DeleteAsync(storageKey); } catch { /* best-effort orphan cleanup */ }
        }

        /// <summary>
        /// Phase 18 — scan the just-stored bytes. Returns the honest <see cref="FileScanStatus"/> to
        /// persist. Infected content is deleted and the upload rejected (400). When the scanner is
        /// enabled but cannot produce a verdict, the upload is either rejected (503, fail-closed) or
        /// recorded as <see cref="FileScanStatus.ScannerUnavailable"/> per policy. When no scanner is
        /// active the bytes are NOT re-read and the status is <see cref="FileScanStatus.NotScanned"/>.
        /// </summary>
        private async Task<FileScanStatus> ScanUploadedAsync(string storageKey, string fileName, string contentType, CancellationToken ct)
        {
            if (!_scanner.IsEnabled)
                return FileScanStatus.NotScanned;

            FileScanResult result;
            try
            {
                await using var stream = await _provider.OpenReadAsync(storageKey, ct);
                result = await _scanner.ScanAsync(stream, fileName, contentType, ct);
            }
            catch (StorageUnavailableException)
            {
                throw; // provider truly down — surface the honest 503, do not mask as a scan verdict
            }
            catch
            {
                result = FileScanResult.Unavailable();
            }

            switch (result.Status)
            {
                case FileScanStatus.Infected:
                    await SafeDeleteBytesAsync(storageKey);
                    throw new BadRequestException(
                        $"File failed the malware scan ({result.Signature ?? "infected"}) and was rejected.");

                case FileScanStatus.ScannerUnavailable when _scanner.RejectOnUnavailable:
                    await SafeDeleteBytesAsync(storageKey);
                    throw new StorageUnavailableException(
                        "Malware scanner is unavailable and policy requires a scan; upload rejected.");

                default:
                    return result.Status;
            }
        }

        private static string Meta(string action, FileRecord r) =>
            $"{{\"action\":\"{action}\",\"purpose\":\"{r.Purpose}\",\"visibility\":\"{r.Visibility}\",\"sizeBytes\":{r.SizeBytes},\"sensitive\":{(r.Visibility == FileVisibility.Sensitive ? "true" : "false")}}}";

        // ---- signed-token (HMAC-SHA256 over "fileId|tenantId|expUnix") ----

        private string Sign(string fileId, string tenantId, DateTime expiresUtc)
        {
            var exp = new DateTimeOffset(expiresUtc, TimeSpan.Zero).ToUnixTimeSeconds();
            var payload = $"{fileId}|{tenantId}|{exp}";
            var sig = ComputeSig(payload);
            return $"{B64Url(Encoding.UTF8.GetBytes(payload))}.{B64Url(sig)}";
        }

        private bool TryVerify(string token, out string fileId, out string tenantId)
        {
            fileId = string.Empty; tenantId = string.Empty;
            if (string.IsNullOrWhiteSpace(token)) return false;
            var parts = token.Split('.');
            if (parts.Length != 2) return false;
            string payload;
            byte[] sig;
            try
            {
                payload = Encoding.UTF8.GetString(FromB64Url(parts[0]));
                sig = FromB64Url(parts[1]);
            }
            catch { return false; }

            var expected = ComputeSig(payload);
            if (!CryptographicOperations.FixedTimeEquals(sig, expected)) return false;

            var fields = payload.Split('|');
            if (fields.Length != 3) return false;
            if (!long.TryParse(fields[2], out var exp)) return false;
            if (DateTimeOffset.FromUnixTimeSeconds(exp) < DateTimeOffset.UtcNow) return false;

            fileId = fields[0];
            tenantId = fields[1];
            return true;
        }

        private byte[] ComputeSig(string payload)
        {
            using var hmac = new HMACSHA256(_signingKey);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        }

        private static string B64Url(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] FromB64Url(string s)
        {
            var b64 = s.Replace('-', '+').Replace('_', '/');
            switch (b64.Length % 4) { case 2: b64 += "=="; break; case 3: b64 += "="; break; }
            return Convert.FromBase64String(b64);
        }
    }
}
