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
    /// Phase 6 §9 — backend-mediated AI tutor. The browser calls this route
    /// (never school-ai-rag directly); the backend authenticates, derives tenant
    /// context from the signed token, calls the internal AI contract, records
    /// usage, and returns a normalized grounded answer with citations.
    /// </summary>
    [ApiController]
    [Route("api")]
    [Authorize(Policy = Policies.TenantMember)]
    [EnableRateLimiting(RateLimitPolicies.Ai)]
    public class AiTutorController : ControllerBase
    {
        private readonly ITutorService _tutor;
        public AiTutorController(ITutorService tutor) => _tutor = tutor;

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] TutorChatRequestDto request, CancellationToken ct)
        {
            var result = await _tutor.AskAsync(request, ct);
            return Ok(result);
        }
    }
}
