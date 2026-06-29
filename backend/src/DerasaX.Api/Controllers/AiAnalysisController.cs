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
    /// Phase 6 §13 — backend-mediated conversation / pain-point analysis. Only a
    /// teacher or school admin may generate or review; access is enforced by the
    /// student-access authorizer. Parents receive an approved-only safe projection
    /// (never the internal analysis). Outputs require human review.
    /// </summary>
    [ApiController]
    [Route("api/v1/ai/analysis")]
    [Authorize(Policy = Policies.TenantMember)]
    [EnableRateLimiting(RateLimitPolicies.Ai)]
    public class AiAnalysisController : ControllerBase
    {
        private readonly IAnalysisService _analysis;
        public AiAnalysisController(IAnalysisService analysis) => _analysis = analysis;

        [HttpPost]
        [Authorize(Policy = Policies.TeacherOrSchoolAdmin)]
        public async Task<IActionResult> Generate([FromBody] GenerateAnalysisDto request, CancellationToken ct)
            => Ok(await _analysis.GenerateAsync(request, ct));

        [HttpGet("{studentId}/history")]
        public async Task<IActionResult> History(string studentId, CancellationToken ct)
            => Ok(await _analysis.GetHistoryForCallerAsync(studentId, ct));

        [HttpPut("{painPointId}/review")]
        [Authorize(Policy = Policies.TeacherOrSchoolAdmin)]
        public async Task<IActionResult> Review(string painPointId, [FromBody] ReviewPainPointDto decision, CancellationToken ct)
        {
            await _analysis.ReviewAsync(painPointId, decision, ct);
            return NoContent();
        }
    }
}
