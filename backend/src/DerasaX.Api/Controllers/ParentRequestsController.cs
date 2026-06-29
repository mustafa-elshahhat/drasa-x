using System;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.CommunicationDto;
using DerasaX.Application.Services.Abstractions.Communication;
using DerasaX.Application.Services.Abstractions.Storage;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 5 §12 (Increment 5) — parent document/contact requests. A parent creates and tracks
    /// requests for their linked children; SchoolAdmin responds and drives the status lifecycle.
    /// A request is visible only to the requesting parent and authorized tenant staff.
    /// Phase 16 adds sensitive document attachments: a linked parent (request owner) or tenant
    /// staff may up/download them; unrelated parents cannot, and every download is audited.
    /// </summary>
    [ApiController]
    [Route("api/v1/parent-requests")]
    [Authorize(Policy = Policies.TenantMember)]
    public class ParentRequestsController : ControllerBase
    {
        private readonly IParentRequestService _service;
        private readonly IFileStorageService _storage;
        public ParentRequestsController(IParentRequestService service, IFileStorageService storage)
        {
            _service = service;
            _storage = storage;
        }

        [HttpPost]
        [Authorize(Policy = Policies.ParentOnly)]
        public async Task<IActionResult> Create([FromBody] CreateParentRequestDto dto, CancellationToken ct)
            => R(await _service.CreateAsync(dto, ct));

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] ParentRequestParameters p, CancellationToken ct)
            => R(await _service.ListAsync(p, ct));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id, CancellationToken ct)
            => R(await _service.GetAsync(id, ct));

        [HttpPost("{id}/responses")]
        [Authorize(Policy = Policies.SchoolAdminOnly)]
        public async Task<IActionResult> Respond(string id, [FromBody] RespondParentRequestDto dto, CancellationToken ct)
            => R(await _service.RespondAsync(id, dto, ct));

        [HttpPost("{id}/status")]
        [Authorize(Policy = Policies.SchoolAdminOnly)]
        public async Task<IActionResult> Transition(string id, [FromBody] TransitionParentRequestDto dto, CancellationToken ct)
            => R(await _service.TransitionAsync(id, dto, ct));

        // ---- Phase 16: sensitive document attachments ----

        /// <summary>Parent attaches a sensitive document to their own request.</summary>
        [HttpPost("{id}/attachment")]
        [Authorize(Policy = Policies.ParentOnly)]
        [RequestSizeLimit(30_000_000)]
        public async Task<IActionResult> AttachRequestDocument(string id, [FromForm] ParentDocumentForm form, CancellationToken ct)
        {
            var record = await UploadSensitiveAsync(form, FilePurpose.ParentDocumentRequest, "ParentRequest", id, ct);
            try
            {
                return R(await _service.AttachRequestDocumentAsync(id, record.Id, ct));
            }
            catch
            {
                await SafeCleanupAsync(record.Id, ct);
                throw;
            }
        }

        /// <summary>Linked parent (owner) or staff downloads the request's attached document. Audited.</summary>
        [HttpGet("{id}/attachment/download")]
        public async Task<IActionResult> DownloadRequestDocument(string id, CancellationToken ct)
        {
            var fileId = await _service.GetAuthorizedRequestDocumentIdAsync(id, ct);
            var result = await _storage.OpenPreAuthorizedAsync(fileId, FilePurpose.ParentDocumentRequest, ct);
            return File(result.Content, result.Record.ContentType, result.Record.FileName);
        }

        /// <summary>Staff responds to a request with a sensitive document.</summary>
        [HttpPost("{id}/response-document")]
        [Authorize(Policy = Policies.SchoolAdminOnly)]
        [RequestSizeLimit(30_000_000)]
        public async Task<IActionResult> AttachResponseDocument(string id, [FromForm] ParentDocumentForm form, CancellationToken ct)
        {
            var record = await UploadSensitiveAsync(form, FilePurpose.ParentDocumentResponse, "ParentRequest", id, ct);
            try
            {
                return R(await _service.AttachResponseDocumentAsync(id, record.Id, form.Body, ct));
            }
            catch
            {
                await SafeCleanupAsync(record.Id, ct);
                throw;
            }
        }

        /// <summary>Linked parent (owner) or staff downloads a response document. Audited.</summary>
        [HttpGet("{id}/responses/{responseId}/document/download")]
        public async Task<IActionResult> DownloadResponseDocument(string id, string responseId, CancellationToken ct)
        {
            var fileId = await _service.GetAuthorizedResponseDocumentIdAsync(id, responseId, ct);
            var result = await _storage.OpenPreAuthorizedAsync(fileId, FilePurpose.ParentDocumentResponse, ct);
            return File(result.Content, result.Record.ContentType, result.Record.FileName);
        }

        private async Task<Domain.Entities.Models.FileRecord> UploadSensitiveAsync(
            ParentDocumentForm form, FilePurpose purpose, string relatedType, string relatedId, CancellationToken ct)
        {
            if (form.File is null || form.File.Length == 0)
                throw new BadRequestException("A non-empty file is required.");
            await using var stream = form.File.OpenReadStream();
            return await _storage.UploadAsync(new FileUploadRequest
            {
                Content = stream,
                OriginalFileName = form.File.FileName,
                DeclaredContentType = form.File.ContentType ?? "application/octet-stream",
                SizeBytes = form.File.Length,
                Purpose = purpose,
                Visibility = FileVisibility.Sensitive,
                RelatedEntityType = relatedType,
                RelatedEntityId = relatedId
            }, ct);
        }

        private async Task SafeCleanupAsync(string fileId, CancellationToken ct)
        {
            try { await _storage.SoftDeleteAsync(fileId, ct); } catch { /* best-effort */ }
        }

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }

    /// <summary>Multipart form for parent-request sensitive documents (Phase 16).</summary>
    public class ParentDocumentForm
    {
        public IFormFile? File { get; set; }
        public string? Body { get; set; }
    }
}
