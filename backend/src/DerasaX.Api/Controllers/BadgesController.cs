using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.EngagementDto;
using DerasaX.Application.Services.Abstractions.Engagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 5 §13 (Increment 6) — badge catalog (platform-owned), student badge listing/award
    /// (idempotent, audited, notified) and streaks. Award is restricted to a teacher (assigned
    /// students) or SchoolAdmin (same-tenant); listing is relationship-authorized.
    /// </summary>
    [ApiController]
    [Route("api/v1")]
    [Authorize(Policy = Policies.TenantMember)]
    public class BadgesController : ControllerBase
    {
        private readonly IBadgeService _service;
        public BadgesController(IBadgeService service) => _service = service;

        [HttpGet("badges")]
        public async Task<IActionResult> Catalog(CancellationToken ct) => R(await _service.CatalogAsync(ct));

        [HttpGet("students/{studentId}/badges")]
        public async Task<IActionResult> StudentBadges(string studentId, CancellationToken ct) => R(await _service.StudentBadgesAsync(studentId, ct));

        [HttpPost("students/{studentId}/badges")]
        public async Task<IActionResult> Award(string studentId, [FromBody] AwardBadgeDto dto, CancellationToken ct) => R(await _service.AwardAsync(studentId, dto, ct));

        [HttpGet("students/{studentId}/streak")]
        public async Task<IActionResult> Streak(string studentId, CancellationToken ct) => R(await _service.StreakAsync(studentId, ct));

        [HttpPost("students/{studentId}/streak")]
        public async Task<IActionResult> UpdateStreak(string studentId, [FromBody] UpdateStreakDto dto, CancellationToken ct) => R(await _service.UpdateStreakAsync(studentId, dto, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
