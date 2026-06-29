using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.OperationsDto;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Api.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 5 §14 (Increment 7) — AI usage recording contract (consumed by Phase 6 orchestration)
    /// plus tenant-scoped usage listing, summaries and plan-limit visibility. Phase 5 records usage
    /// metadata only; it never executes AI requests.
    /// </summary>
    [ApiController]
    [Route("api/v1/ai-usage")]
    [Authorize(Policy = Policies.SchoolAdminOnly)]
    [EnableRateLimiting(RateLimitPolicies.Ai)]
    public class AiUsageController : ControllerBase
    {
        private readonly IAiUsageService _service;
        public AiUsageController(IAiUsageService service) => _service = service;

        [HttpPost]
        public async Task<IActionResult> Record([FromBody] RecordAiUsageDto dto, CancellationToken ct) => R(await _service.RecordAsync(dto, ct));

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] AiUsageParameters p, CancellationToken ct) => R(await _service.ListAsync(p, ct));

        [HttpGet("summary")]
        public async Task<IActionResult> Summary(CancellationToken ct) => R(await _service.SummaryAsync(ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
