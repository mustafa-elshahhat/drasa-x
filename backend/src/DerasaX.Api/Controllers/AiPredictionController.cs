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
    /// Phase 6 §12 — backend-mediated performance prediction. Access is enforced
    /// by the student-access authorizer (self / assigned teacher / linked parent /
    /// same-tenant admin; cross-tenant → 404). The browser sends only the target
    /// student id; the backend derives features from authoritative records.
    /// </summary>
    [ApiController]
    [Route("api/v1/ai/prediction")]
    [Authorize(Policy = Policies.TenantMember)]
    [EnableRateLimiting(RateLimitPolicies.Ai)]
    public class AiPredictionController : ControllerBase
    {
        private readonly IPredictionService _prediction;
        public AiPredictionController(IPredictionService prediction) => _prediction = prediction;

        [HttpPost]
        public async Task<IActionResult> Generate([FromBody] GeneratePredictionDto request, CancellationToken ct)
            => Ok(await _prediction.GenerateAsync(request, ct));

        [HttpGet("{studentId}/history")]
        public async Task<IActionResult> History(string studentId, CancellationToken ct)
            => Ok(await _prediction.GetHistoryAsync(studentId, ct));

        [HttpPut("{studentId}/learning-profile")]
        [Authorize(Policy = Policies.TeacherOrSchoolAdmin)]
        public async Task<IActionResult> SetProfile(string studentId, [FromBody] SetLearningProfileDto profile, CancellationToken ct)
        {
            await _prediction.UpsertLearningProfileAsync(studentId, profile, ct);
            return NoContent();
        }
    }
}
