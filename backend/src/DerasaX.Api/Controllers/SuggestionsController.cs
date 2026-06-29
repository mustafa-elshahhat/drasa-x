using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.CommunicationDto;
using DerasaX.Application.Services.Abstractions.Communication;
using DerasaX.Api.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 5 §12 (Increment 5) — anonymous suggestion box. Any tenant member may submit; the
    /// author identity is never returned through the staff-facing list/moderation APIs. SchoolAdmin
    /// lists and moderates; moderation actions are audited.
    /// </summary>
    [ApiController]
    [Route("api/v1/suggestions")]
    [Authorize(Policy = Policies.TenantMember)]
    [EnableRateLimiting(RateLimitPolicies.Messaging)]
    public class SuggestionsController : ControllerBase
    {
        private readonly ISuggestionService _service;
        public SuggestionsController(ISuggestionService service) => _service = service;

        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] SubmitSuggestionDto dto, CancellationToken ct)
            => R(await _service.SubmitAsync(dto, ct));

        [HttpGet]
        [Authorize(Policy = Policies.SchoolAdminOnly)]
        public async Task<IActionResult> List([FromQuery] SuggestionParameters p, CancellationToken ct)
            => R(await _service.ListAsync(p, ct));

        [HttpPost("{id}/moderate")]
        [Authorize(Policy = Policies.SchoolAdminOnly)]
        public async Task<IActionResult> Moderate(string id, [FromBody] ModerateSuggestionDto dto, CancellationToken ct)
            => R(await _service.ModerateAsync(id, dto, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
