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
    /// Phase 5 §13 (Increment 6) — competitions: lifecycle, student entries (eligibility +
    /// duplicate-entry prevention), authoritative score recording (staff only) and leaderboard
    /// with result-visibility timing. Tenant-scoped.
    /// </summary>
    [ApiController]
    [Route("api/v1/competitions")]
    [Authorize(Policy = Policies.TenantMember)]
    public class CompetitionsController : ControllerBase
    {
        private readonly ICompetitionService _service;
        public CompetitionsController(ICompetitionService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] CompetitionParameters p, CancellationToken ct) => R(await _service.ListAsync(p, ct));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id, CancellationToken ct) => R(await _service.GetAsync(id, ct));

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCompetitionDto dto, CancellationToken ct) => R(await _service.CreateAsync(dto, ct));

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateCompetitionDto dto, CancellationToken ct) => R(await _service.UpdateAsync(id, dto, ct));

        [HttpPost("{id}/publish")]
        public async Task<IActionResult> Publish(string id, CancellationToken ct) => R(await _service.PublishAsync(id, ct));

        [HttpPost("{id}/archive")]
        public async Task<IActionResult> Archive(string id, CancellationToken ct) => R(await _service.ArchiveAsync(id, ct));

        [HttpPost("{id}/close")]
        public async Task<IActionResult> Close(string id, CancellationToken ct) => R(await _service.CloseAsync(id, ct));

        [HttpPost("{id}/entries")]
        public async Task<IActionResult> Enter(string id, CancellationToken ct) => R(await _service.EnterAsync(id, ct));

        [HttpPost("{id}/entries/{entryId}/score")]
        public async Task<IActionResult> RecordScore(string id, string entryId, [FromBody] RecordScoreDto dto, CancellationToken ct)
            => R(await _service.RecordScoreAsync(id, entryId, dto, ct));

        [HttpGet("{id}/leaderboard")]
        public async Task<IActionResult> Leaderboard(string id, CancellationToken ct) => R(await _service.LeaderboardAsync(id, ct));

        // Phase 14 (closure) — durable competition submissions: a student submits/updates work; staff judge.
        [HttpPost("{id}/submissions")]
        public async Task<IActionResult> Submit(string id, [FromBody] SubmitCompetitionDto dto, CancellationToken ct) => R(await _service.SubmitAsync(id, dto, ct));

        [HttpGet("{id}/submissions/me")]
        public async Task<IActionResult> MySubmission(string id, CancellationToken ct) => R(await _service.MySubmissionAsync(id, ct));

        [HttpGet("{id}/submissions")]
        public async Task<IActionResult> Submissions(string id, CancellationToken ct) => R(await _service.SubmissionsAsync(id, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
