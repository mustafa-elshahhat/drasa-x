using System.Threading;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.LessonDto;
using DerasaX.Application.Dto.LessonMaterialDto;
using DerasaX.Application.Services.Abstractions.LessonMaterial;
using DerasaX.Application.Services.Abstractions.Storage;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    [Route("api/v1/[controller]")]   // canonical versioned route (Phase 22 Step 6)
    [Route("api/[controller]")]      // legacy alias — retained for backwards compatibility during /api/v1 convergence
    [ApiController]
    [Authorize(Policy = Policies.TenantMember)]
    public class LessonMaterialController : ControllerBase
    {
        private readonly ILessonMaterialServicess _lessonMaterialServicess;
        private readonly IFileStorageService _storage;
        private readonly ILogger<LessonMaterialController> _logger;

        public LessonMaterialController(ILessonMaterialServicess lessonMaterialServicess, IFileStorageService storage, ILogger<LessonMaterialController> logger)
        {
            _lessonMaterialServicess=lessonMaterialServicess;
            _storage=storage;
            _logger=logger;
        }

        /// <summary>
        /// Phase 16 — teacher/admin uploads a durable lesson-material file (bytes + metadata) and
        /// links it to the lesson. The material is tenant-internal, so any member of the tenant
        /// (e.g. an enrolled student) can download it via the file API. Replaces the URL-only path
        /// for real attachments.
        /// </summary>
        [HttpPost("UploadMaterial")]
        [Authorize(Policy = Policies.TeacherOnly)]
        [RequestSizeLimit(110_000_000)]
        public async Task<IActionResult> UploadMaterial([FromForm] UploadLessonMaterialForm form, CancellationToken ct)
        {
            if (form.File is null || form.File.Length == 0)
                throw new BadRequestException("A non-empty file is required.");
            if (string.IsNullOrWhiteSpace(form.LessonId) || string.IsNullOrWhiteSpace(form.Title))
                throw new BadRequestException("LessonId and Title are required.");

            await using var stream = form.File.OpenReadStream();
            var record = await _storage.UploadAsync(new FileUploadRequest
            {
                Content = stream,
                OriginalFileName = form.File.FileName,
                DeclaredContentType = form.File.ContentType ?? "application/octet-stream",
                SizeBytes = form.File.Length,
                Purpose = FilePurpose.LessonMaterial,
                RelatedEntityType = "Lesson",
                RelatedEntityId = form.LessonId
            }, ct);

            var result = await _lessonMaterialServicess.AddUploadedMaterialAsync(
                form.LessonId, form.Title, form.Type, record.Id, $"/api/v1/files/{record.Id}/download");
            _logger.LogInformation("Uploaded lesson material file {FileId} for lesson {LessonId}", record.Id, form.LessonId);
            return StatusCode(result.StatusCode, result);
        }
        [HttpGet("GetMaterialByLessonId")]
        public async Task<IActionResult> GetMaterialByLessonId(string id)
        {
            _logger.LogInformation("Getting material by lesson ID: {lessonId}", id);

            var result = await _lessonMaterialServicess.GetMaterialByLessonIdAsync(id);

            _logger.LogInformation("Successfully retrieved material for lesson ID: {unitId}", id);
            return Ok(result);
        }

        /// <summary>
        /// Fetches a single lesson material by its own id (no parent lesson id required). Added for
        /// detail pages such as the student material page (`/app/student/materials/:materialId`)
        /// that only have the material id in the URL. Inherits the class-level TenantMember policy —
        /// any member of the tenant may read, matching <see cref="UploadMaterial"/>'s doc comment.
        /// A bare "{id}" route coexists safely with the literal "GetMaterialByLessonId" route above
        /// (and the other literal-named GET/POST/PUT/DELETE actions below): ASP.NET Core route
        /// precedence always prefers an all-literal template over a parameterized one for the same
        /// URL, which is the exact pattern already proven in this codebase by FilesController's
        /// "download" (literal) vs "{id}" (legacy Get) GET routes.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMaterialById(string id)
        {
            _logger.LogInformation("Getting material by ID: {MaterialId}", id);

            var result = await _lessonMaterialServicess.GetMaterialByIdAsync(id);

            _logger.LogInformation("Successfully retrieved material with ID: {MaterialId}", id);
            return Ok(result);
        }
        [HttpPost("AddMaterial")]
        [Authorize(Policy = Policies.TeacherOnly)]
        public async Task<IActionResult> AddMaterial([FromForm] AddLessonMaterialDto lessonMaterialDto)
        {
            _logger.LogInformation("Adding Material");

            var result = await _lessonMaterialServicess.AddMaterialAsync(lessonMaterialDto);

            _logger.LogInformation("Successfully added material with ID: {materialId}", result.Data?.Id);
            return Ok(result);
        }

        [HttpPut("UpdateMaterial")]
        [Authorize(Policy = Policies.TeacherOnly)]
        public async Task<IActionResult> UpdateMaterial([FromForm] GetLessonMaterialDto getLessonMaterialDto)
        {
            _logger.LogInformation("Updating material with ID: {mterialId}", getLessonMaterialDto.Id);

            var result = await _lessonMaterialServicess.UpdateMaterialAsync(getLessonMaterialDto);

            _logger.LogInformation("Successfully updated material with ID: {materialId}", getLessonMaterialDto.Id);
            return Ok(result);
        }

        [HttpDelete("DeleteMaterial")]
        [Authorize(Policy = Policies.TeacherOnly)]
        public async Task<IActionResult> DeleteMaterial(string id)
        {
            _logger.LogInformation("Deleting material with ID: {MaterialId}", id);

            var result = await _lessonMaterialServicess.DeleteMaterial(id);

            _logger.LogInformation("Successfully deleted material with ID: {materialId}", id);
            return Ok(result);
        }
    }

    /// <summary>Multipart form for uploading a durable lesson-material file (Phase 16).</summary>
    public class UploadLessonMaterialForm
    {
        public IFormFile? File { get; set; }
        public string LessonId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public AttachmentType Type { get; set; } = AttachmentType.Document;
    }
}
