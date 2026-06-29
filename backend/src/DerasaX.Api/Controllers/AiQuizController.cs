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
    /// Phase 6 §11 — backend-mediated AI quiz-draft generation. A teacher (or
    /// school admin) requests a grounded draft; the backend authorizes, calls
    /// school-ai-rag, validates, and persists a Draft/Origin=AiGenerated quiz that
    /// requires teacher review before publishing. The browser never calls the AI
    /// service directly, and correct answers are never returned to students.
    /// </summary>
    [ApiController]
    [Route("api/v1/ai/quiz")]
    [Authorize(Policy = Policies.TeacherOrSchoolAdmin)]
    [EnableRateLimiting(RateLimitPolicies.Ai)]
    public class AiQuizController : ControllerBase
    {
        private readonly IQuizDraftService _drafts;
        public AiQuizController(IQuizDraftService drafts) => _drafts = drafts;

        [HttpPost("draft")]
        public async Task<IActionResult> Draft([FromBody] GenerateQuizDraftDto request, CancellationToken ct)
            => Ok(await _drafts.GenerateDraftAsync(request, ct));
    }
}
