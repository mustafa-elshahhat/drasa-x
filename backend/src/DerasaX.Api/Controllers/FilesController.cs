using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.OperationsDto;
using DerasaX.Application.Dto.StorageDto;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Application.Services.Abstractions.Storage;
using DerasaX.Api.RateLimiting;
using DerasaX.Domain.Common;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 16 — durable, tenant-isolated file API. Generic upload is limited to self-service,
    /// non-relationship purposes; relationship-sensitive files (lesson materials, parent documents,
    /// CV enrollment assets) are uploaded/downloaded through their purpose-specific controllers,
    /// which perform the relationship authorization first. Every download is backend-mediated and
    /// audited; signed tokens are HMAC-signed and expire. The legacy Phase 5 metadata endpoints are
    /// retained for backward compatibility.
    /// </summary>
    [ApiController]
    [Route("api/v1/files")]
    [Authorize(Policy = Policies.TenantMember)]
    [EnableRateLimiting(RateLimitPolicies.Files)]
    public class FilesController : ControllerBase
    {
        private readonly IFileMetadataService _metadata;
        private readonly IFileStorageService _storage;

        public FilesController(IFileMetadataService metadata, IFileStorageService storage)
        {
            _metadata = metadata;
            _storage = storage;
        }

        // Purposes that MUST go through a purpose-specific endpoint that performs relationship authz.
        private static readonly HashSet<FilePurpose> RestrictedFromGenericUpload = new()
        {
            FilePurpose.LessonMaterial,
            FilePurpose.ParentDocumentRequest,
            FilePurpose.ParentDocumentResponse,
            FilePurpose.CvEnrollmentAsset
        };

        // ---- Phase 16 durable storage ----

        /// <summary>Uploads a self-service file (the bytes + validated metadata) for the current tenant.</summary>
        [HttpPost("upload")]
        [RequestSizeLimit(110_000_000)] // hard ceiling above the largest per-purpose cap (100 MB)
        public async Task<IActionResult> Upload([FromForm] FileUploadForm form, CancellationToken ct)
        {
            if (form.File is null || form.File.Length == 0)
                throw new BadRequestException("A non-empty file is required.");
            if (RestrictedFromGenericUpload.Contains(form.Purpose))
                throw new BadRequestException($"{form.Purpose} files must be uploaded via their dedicated endpoint.");

            await using var stream = form.File.OpenReadStream();
            var record = await _storage.UploadAsync(new FileUploadRequest
            {
                Content = stream,
                OriginalFileName = form.File.FileName,
                DeclaredContentType = form.File.ContentType ?? "application/octet-stream",
                SizeBytes = form.File.Length,
                Purpose = form.Purpose,
                RelatedEntityType = form.RelatedEntityType,
                RelatedEntityId = form.RelatedEntityId
            }, ct);

            return StatusCode(201, new ApiResponse<StoredFileDto>(StoredFileDto.From(record))
            { Success = true, StatusCode = 201, Message = "File uploaded." });
        }

        /// <summary>Returns client-safe metadata (tenant-isolated; cross-tenant ⇒ 404).</summary>
        [HttpGet("{id}/metadata")]
        public async Task<IActionResult> Metadata(string id, CancellationToken ct)
        {
            var record = await _storage.GetMetadataAsync(id, ct);
            return Ok(new ApiResponse<StoredFileDto>(StoredFileDto.From(record)) { Success = true, StatusCode = 200, Message = "OK" });
        }

        /// <summary>Streams the file with baseline authorization (owner/admin/tenant-internal). Audited.</summary>
        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(string id, CancellationToken ct)
        {
            var result = await _storage.OpenWithBaselineAuthorizationAsync(id, ct);
            return File(result.Content, result.Record.ContentType, result.Record.FileName);
        }

        /// <summary>Issues a short-lived, HMAC-signed download token for an authorized file.</summary>
        [HttpPost("{id}/signed-download")]
        public async Task<IActionResult> SignedDownload(string id, CancellationToken ct)
        {
            var signed = await _storage.CreateSignedDownloadTokenAsync(id, null, ct);
            var dto = new SignedDownloadDto
            {
                Token = signed.Token,
                ExpiresAtUtc = signed.ExpiresAtUtc,
                DownloadUrl = $"/api/v1/files/download?token={Uri.EscapeDataString(signed.Token)}"
            };
            return Ok(new ApiResponse<SignedDownloadDto>(dto) { Success = true, StatusCode = 200, Message = "Signed download issued." });
        }

        /// <summary>Redeems a signed token and streams the file. Anonymous (the token is the credential); audited.</summary>
        [HttpGet("download")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadBySignedToken([FromQuery] string token, CancellationToken ct)
        {
            var result = await _storage.OpenBySignedTokenAsync(token, ct);
            return File(result.Content, result.Record.ContentType, result.Record.FileName);
        }

        /// <summary>Soft-deletes a file (owner/admin only). Future downloads return 404. Audited.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken ct)
        {
            await _storage.SoftDeleteAsync(id, ct);
            return Ok(new ApiResponse<bool>(true) { Success = true, StatusCode = 200, Message = "File deleted." });
        }

        // ---- Phase 5 legacy metadata contracts (retained) ----

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFileRecordDto dto, CancellationToken ct) => R(await _metadata.CreateAsync(dto, ct));

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] FileParameters p, CancellationToken ct) => R(await _metadata.ListAsync(p, ct));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id, CancellationToken ct) => R(await _metadata.GetAsync(id, ct));

        [HttpPost("{id}/archive")]
        public async Task<IActionResult> Archive(string id, CancellationToken ct) => R(await _metadata.ArchiveAsync(id, ct));

        private IActionResult R<T>(ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }

    /// <summary>Multipart form for the generic upload endpoint.</summary>
    public class FileUploadForm
    {
        public IFormFile? File { get; set; }
        public FilePurpose Purpose { get; set; } = FilePurpose.Other;
        public string? RelatedEntityType { get; set; }
        public string? RelatedEntityId { get; set; }
    }
}
