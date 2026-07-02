using System.Threading;
using System.Threading.Tasks;
using DerasaX.Api.RateLimiting;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.AssessmentDto;
using DerasaX.Application.Services.Abstractions.Assessment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 5 closure — homework / general (non-quiz) assignment lifecycle. Teacher-owned:
    /// a teacher authors, publishes, reviews and grades their own homework (SchoolAdmin
    /// Teacher-portal removal — homework has no school-admin equivalent surface); students
    /// list, submit and view their own submission. Tenant-isolated (cross-tenant ids → 404);
    /// relationship/role gated per action.
    /// </summary>
    [ApiController]
    [Route("api/v1/homework")]
    [Authorize(Policy = Policies.TenantMember)]
    public class HomeworkController : ControllerBase
    {
        private readonly IHomeworkService _service;
        public HomeworkController(IHomeworkService service) => _service = service;

        // ---- Teacher ----

        [HttpPost]
        [Authorize(Policy = Policies.TeacherOnly)]
        public async Task<IActionResult> Create([FromBody] CreateHomeworkDto dto, CancellationToken ct)
            => R(await _service.CreateDraftAsync(dto, ct));

        [HttpGet]
        [Authorize(Policy = Policies.TeacherOnly)]
        public async Task<IActionResult> ListMine(CancellationToken ct)
            => R(await _service.ListMineAsync(ct));

        [HttpPut("{id}")]
        [Authorize(Policy = Policies.TeacherOnly)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateHomeworkDto dto, CancellationToken ct)
            => R(await _service.UpdateAsync(id, dto, ct));

        [HttpPost("{id}/publish")]
        [Authorize(Policy = Policies.TeacherOnly)]
        public async Task<IActionResult> Publish(string id, [FromBody] PublishHomeworkDto dto, CancellationToken ct)
            => R(await _service.PublishAsync(id, dto, ct));

        [HttpGet("{id}")]
        [Authorize(Policy = Policies.TeacherOnly)]
        public async Task<IActionResult> Get(string id, CancellationToken ct)
            => R(await _service.GetAsync(id, ct));

        [HttpGet("{id}/submissions")]
        [Authorize(Policy = Policies.TeacherOnly)]
        public async Task<IActionResult> Submissions(string id, [FromQuery] HomeworkSubmissionParameters p, CancellationToken ct)
        {
            var r = await _service.ListSubmissionsAsync(id, p, ct);
            return StatusCode(r.StatusCode, r);
        }

        [HttpPost("submissions/{submissionId}/grade")]
        [Authorize(Policy = Policies.TeacherOnly)]
        public async Task<IActionResult> Grade(string submissionId, [FromBody] GradeHomeworkDto dto, CancellationToken ct)
            => R(await _service.GradeAsync(submissionId, dto, ct));

        // ---- Student ----

        [HttpGet("assigned")]
        [Authorize(Policy = Policies.StudentOnly)]
        public async Task<IActionResult> Assigned(CancellationToken ct)
            => R(await _service.ListAssignedAsync(ct));

        [HttpPost("{id}/submit")]
        [Authorize(Policy = Policies.StudentOnly)]
        [EnableRateLimiting(RateLimitPolicies.Submission)]
        public async Task<IActionResult> Submit(string id, [FromBody] SubmitHomeworkDto dto, CancellationToken ct)
            => R(await _service.SubmitAsync(id, dto, ct));

        [HttpGet("{id}/my-submission")]
        [Authorize(Policy = Policies.StudentOnly)]
        public async Task<IActionResult> MySubmission(string id, CancellationToken ct)
            => R(await _service.GetMySubmissionAsync(id, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
