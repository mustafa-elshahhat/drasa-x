using System.Threading;
using System.Threading.Tasks;
using DerasaX.Api.RateLimiting;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.AiDto;
using DerasaX.Application.Services.Abstractions.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 6 §7 — backend-mediated tenant-scoped curriculum ingestion. Teachers
    /// and school admins register/extract curriculum; the backend forwards trusted
    /// document + curriculum metadata to school-ai-rag over the authenticated
    /// internal contract. The browser never calls the AI service directly.
    /// </summary>
    [ApiController]
    [Route("api/v1/ai/documents")]
    [Authorize(Policy = Policies.TeacherOrSchoolAdmin)]
    [EnableRateLimiting(RateLimitPolicies.Ai)]
    public class AiDocumentsController : ControllerBase
    {
        private readonly IAiDocumentService _documents;
        public AiDocumentsController(IAiDocumentService documents) => _documents = documents;

        [HttpPost]
        public async Task<IActionResult> Ingest([FromBody] IngestCurriculumDocumentDto request, CancellationToken ct)
            => Ok(await _documents.IngestAsync(request, ct));

        [HttpDelete("{documentId}")]
        public async Task<IActionResult> Delete(string documentId, CancellationToken ct)
            => Ok(await _documents.DeleteAsync(documentId, ct));
    }
}
