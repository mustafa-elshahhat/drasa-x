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
    /// Phase 14 — ledger-based gamification. A student's points are the SUM of immutable ledger
    /// transactions; manual awards are restricted to a teacher (assigned students) or SchoolAdmin
    /// (same-tenant) and are audited/notified; the points leaderboard is tenant-scoped (and optionally
    /// grade-scoped). Gamification rules are managed by a school administrator.
    /// </summary>
    [ApiController]
    [Route("api/v1")]
    [Authorize(Policy = Policies.TenantMember)]
    public class GamificationController : ControllerBase
    {
        private readonly IGamificationService _service;
        public GamificationController(IGamificationService service) => _service = service;

        [HttpGet("students/{studentId}/points")]
        public async Task<IActionResult> Summary(string studentId, CancellationToken ct) => R(await _service.SummaryAsync(studentId, ct));

        [HttpGet("students/{studentId}/points/ledger")]
        public async Task<IActionResult> Ledger(string studentId, [FromQuery] PointLedgerParameters p, CancellationToken ct) => R(await _service.LedgerAsync(studentId, p, ct));

        [HttpPost("students/{studentId}/points")]
        public async Task<IActionResult> Award(string studentId, [FromBody] ManualAwardPointsDto dto, CancellationToken ct) => R(await _service.AwardManualAsync(studentId, dto, ct));

        [HttpGet("gamification/leaderboard")]
        public async Task<IActionResult> Leaderboard([FromQuery] PointLeaderboardParameters p, CancellationToken ct) => R(await _service.LeaderboardAsync(p, ct));

        [HttpGet("gamification/rules")]
        public async Task<IActionResult> Rules(CancellationToken ct) => R(await _service.RulesAsync(ct));

        [HttpPut("gamification/rules")]
        public async Task<IActionResult> UpsertRule([FromBody] UpsertGamificationRuleDto dto, CancellationToken ct) => R(await _service.UpsertRuleAsync(dto, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
