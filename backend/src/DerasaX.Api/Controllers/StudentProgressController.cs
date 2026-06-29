using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.ProgressDto;
using DerasaX.Application.Services.Abstractions.Progress;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 5 §11 (Increment 4) — per-student progress and insight reads. Open to any tenant
    /// member; the per-student relationship rule (self / assigned-teacher / linked-parent /
    /// same-tenant SchoolAdmin) is enforced in the service via the shared
    /// <c>IStudentAccessAuthorizer</c>. A platform SystemAdmin cannot reach these tenant routes.
    /// Insight/prediction endpoints return STORED AI outputs only — no AI runs on read.
    /// </summary>
    [ApiController]
    [Route("api/v1/students/{studentId}")]
    [Authorize(Policy = Policies.TenantMember)]
    public class StudentProgressController : ControllerBase
    {
        private readonly IStudentProgressService _service;
        public StudentProgressController(IStudentProgressService service) => _service = service;

        [HttpGet("lesson-progress")]
        public async Task<IActionResult> LessonProgress(string studentId, [FromQuery] ProgressParameters p, CancellationToken ct)
            => R(await _service.LessonProgressAsync(studentId, p, ct));

        [HttpGet("subject-progress")]
        public async Task<IActionResult> SubjectProgress(string studentId, CancellationToken ct)
            => R(await _service.SubjectProgressAsync(studentId, ct));

        [HttpGet("progress-summary")]
        public async Task<IActionResult> Summary(string studentId, CancellationToken ct)
            => R(await _service.SummaryAsync(studentId, ct));

        [HttpGet("metric-history")]
        public async Task<IActionResult> MetricHistory(string studentId, [FromQuery] MetricHistoryParameters p, CancellationToken ct)
            => R(await _service.MetricHistoryAsync(studentId, p, ct));

        [HttpGet("attempt-history")]
        public async Task<IActionResult> AttemptHistory(string studentId, [FromQuery] ProgressParameters p, CancellationToken ct)
            => R(await _service.AttemptHistoryAsync(studentId, p, ct));

        [HttpGet("insights")]
        public async Task<IActionResult> Insights(string studentId, CancellationToken ct)
            => R(await _service.InsightsAsync(studentId, ct));

        [HttpGet("pain-points")]
        public async Task<IActionResult> PainPoints(string studentId, CancellationToken ct)
            => R(await _service.PainPointsAsync(studentId, ct));

        [HttpGet("recommendations")]
        public async Task<IActionResult> Recommendations(string studentId, CancellationToken ct)
            => R(await _service.RecommendationsAsync(studentId, ct));

        [HttpGet("predictions")]
        public async Task<IActionResult> Predictions(string studentId, CancellationToken ct)
            => R(await _service.PredictionsAsync(studentId, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
