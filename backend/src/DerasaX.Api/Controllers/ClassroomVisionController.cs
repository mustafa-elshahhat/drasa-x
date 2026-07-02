using System;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.VisionDto;
using DerasaX.Application.Services.Abstractions.Vision;
using DerasaX.Application.Services.Abstractions.Storage;
using DerasaX.Domain.Common;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 15 — Teacher-only computer-vision attendance + engagement (SchoolAdmin
    /// Teacher-portal removal — this is a Teacher portal surface with no school-admin
    /// equivalent). Every route is tenant-scoped and role-authorized; the AI service is
    /// reached only through the backend (no direct browser→AI calls). CV never auto-marks
    /// attendance. Phase 16 adds OPTIONAL consented durable enrollment assets, gated OFF by
    /// default (Cv:EnrollmentAssetsEnabled) and honest about being disabled until product
    /// approval. Raw classroom frames are never stored.
    /// </summary>
    [ApiController]
    [Route("api/v1/vision")]
    [Authorize(Policy = Policies.TeacherOnly)]
    public class ClassroomVisionController : ControllerBase
    {
        private readonly IClassroomVisionService _service;
        private readonly IFileStorageService _storage;
        private readonly IConfiguration _config;

        public ClassroomVisionController(IClassroomVisionService service, IFileStorageService storage, IConfiguration config)
        {
            _service = service;
            _storage = storage;
            _config = config;
        }

        private bool EnrollmentAssetsEnabled => _config.GetValue("Cv:EnrollmentAssetsEnabled", false);

        [HttpPost("sessions")]
        public async Task<IActionResult> Start([FromBody] StartVisionSessionDto dto, CancellationToken ct)
            => Respond(await _service.StartSessionAsync(dto, ct));

        [HttpGet("sessions")]
        public async Task<IActionResult> ListSessions([FromQuery] VisionSessionParameters p, CancellationToken ct)
            => Respond(await _service.ListSessionsAsync(p, ct));

        [HttpGet("sessions/{id}")]
        public async Task<IActionResult> GetSession(string id, CancellationToken ct)
            => Respond(await _service.GetSessionAsync(id, ct));

        [HttpPost("sessions/{id}/end")]
        public async Task<IActionResult> End(string id, CancellationToken ct)
            => Respond(await _service.EndSessionAsync(id, ct));

        [HttpPost("sessions/{id}/analyze")]
        public async Task<IActionResult> Analyze(string id, [FromBody] AnalyzeFrameDto dto, CancellationToken ct)
            => Respond(await _service.AnalyzeFrameAsync(id, dto, ct));

        [HttpGet("sessions/{id}/frames")]
        public async Task<IActionResult> Frames(string id, [FromQuery] PaginationParameters p, CancellationToken ct)
            => Respond(await _service.ListFrameAnalysesAsync(id, p, ct));

        [HttpGet("sessions/{id}/candidates")]
        public async Task<IActionResult> Candidates(string id, [FromQuery] VisionCandidateParameters p, CancellationToken ct)
            => Respond(await _service.ListCandidatesAsync(id, p, ct));

        [HttpGet("sessions/{id}/summary")]
        public async Task<IActionResult> Summary(string id, CancellationToken ct)
            => Respond(await _service.GetSessionSummaryAsync(id, ct));

        [HttpPost("candidates/{candidateId}/confirm")]
        public async Task<IActionResult> Confirm(string candidateId, [FromBody] ConfirmCandidateDto dto, CancellationToken ct)
            => Respond(await _service.ConfirmCandidateAsync(candidateId, dto, ct));

        [HttpPost("candidates/{candidateId}/reject")]
        public async Task<IActionResult> Reject(string candidateId, [FromBody] RejectCandidateDto dto, CancellationToken ct)
            => Respond(await _service.RejectCandidateAsync(candidateId, dto, ct));

        [HttpPost("candidates/{candidateId}/override")]
        public async Task<IActionResult> Override(string candidateId, [FromBody] OverrideCandidateDto dto, CancellationToken ct)
            => Respond(await _service.OverrideCandidateAsync(candidateId, dto, ct));

        [HttpPost("enrollments")]
        public async Task<IActionResult> Enroll([FromBody] EnrollFaceDto dto, CancellationToken ct)
            => Respond(await _service.EnrollFaceAsync(dto, ct));

        [HttpGet("enrollments")]
        public async Task<IActionResult> Enrollments([FromQuery] PaginationParameters p, CancellationToken ct)
            => Respond(await _service.ListEnrollmentsAsync(p, ct));

        // ---- Phase 16: optional consented enrollment assets (default OFF) ----

        /// <summary>Honest capability state so the UI can show enabled vs. disabled-pending-approval.</summary>
        [HttpGet("enrollment-assets/status")]
        public IActionResult EnrollmentAssetStatus() =>
            Ok(new ApiResponse<object>(new { enabled = EnrollmentAssetsEnabled, requiresConsent = true })
            { Success = true, StatusCode = 200, Message = EnrollmentAssetsEnabled ? "Enabled." : "Disabled pending product approval." });

        /// <summary>
        /// Stores a CONSENTED enrollment reference image for an existing enrollment. Disabled by
        /// default — returns an honest 403 until <c>Cv:EnrollmentAssetsEnabled</c> is turned on by
        /// product. Requires explicit consent; sets a retention deadline; never stores raw frames.
        /// </summary>
        [HttpPost("enrollments/{enrollmentId}/asset")]
        [RequestSizeLimit(11_000_000)]
        public async Task<IActionResult> UploadEnrollmentAsset(string enrollmentId, [FromForm] CvEnrollmentAssetForm form, CancellationToken ct)
        {
            if (!EnrollmentAssetsEnabled)
                throw new ForbiddenException("CV enrollment asset storage is disabled pending product approval.");
            if (form.File is null || form.File.Length == 0)
                throw new BadRequestException("A non-empty image is required.");
            if (!form.ConsentObtained)
                throw new BadRequestException("Explicit consent is required to store a CV enrollment asset.");

            var retentionUntil = form.RetentionDays.HasValue && form.RetentionDays.Value > 0
                ? DateTime.UtcNow.AddDays(form.RetentionDays.Value)
                : (DateTime?)null;

            await using var stream = form.File.OpenReadStream();
            var record = await _storage.UploadAsync(new FileUploadRequest
            {
                Content = stream,
                OriginalFileName = form.File.FileName,
                DeclaredContentType = form.File.ContentType ?? "application/octet-stream",
                SizeBytes = form.File.Length,
                Purpose = FilePurpose.CvEnrollmentAsset,
                Visibility = FileVisibility.Sensitive,
                RelatedEntityType = "StudentFaceEnrollment",
                RelatedEntityId = enrollmentId,
                ConsentObtained = true,
                ConsentReference = form.ConsentReference,
                RetentionUntil = retentionUntil
            }, ct);

            try
            {
                return Respond(await _service.AttachEnrollmentAssetAsync(
                    enrollmentId, record.Id, true, form.ConsentReference, retentionUntil, ct));
            }
            catch
            {
                try { await _storage.SoftDeleteAsync(record.Id, ct); } catch { /* best-effort */ }
                throw;
            }
        }

        /// <summary>Downloads the consented enrollment asset (staff only, tenant-scoped). Audited.</summary>
        [HttpGet("enrollments/{enrollmentId}/asset/download")]
        public async Task<IActionResult> DownloadEnrollmentAsset(string enrollmentId, CancellationToken ct)
        {
            if (!EnrollmentAssetsEnabled)
                throw new ForbiddenException("CV enrollment asset storage is disabled pending product approval.");
            var fileId = await _service.GetAuthorizedEnrollmentAssetIdAsync(enrollmentId, ct);
            var result = await _storage.OpenPreAuthorizedAsync(fileId, FilePurpose.CvEnrollmentAsset, ct);
            return File(result.Content, result.Record.ContentType, result.Record.FileName);
        }

        private IActionResult Respond<T>(ApiResponse<T> result) => StatusCode(result.StatusCode, result);
    }

    /// <summary>Multipart form for a consented CV enrollment asset (Phase 16).</summary>
    public class CvEnrollmentAssetForm
    {
        public IFormFile? File { get; set; }
        public bool ConsentObtained { get; set; }
        public string? ConsentReference { get; set; }
        public int? RetentionDays { get; set; }
    }
}
