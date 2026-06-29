using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions.Progress;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 5 §11 (Increment 4) — dashboards and performance aggregations. All aggregation is
    /// server-side from authoritative assessment records. The dashboard list is scoped to the
    /// caller's permitted students; class/subject performance is restricted to assigned teachers
    /// and SchoolAdmin. Quiz/question-level performance is served by the quiz analytics endpoint
    /// (<c>GET /api/v1/quizzes/{id}/analytics</c>).
    /// </summary>
    [ApiController]
    [Route("api/v1")]
    [Authorize(Policy = Policies.TenantMember)]
    public class PerformanceController : ControllerBase
    {
        private readonly IPerformanceService _service;
        public PerformanceController(IPerformanceService service) => _service = service;

        [HttpGet("me/students")]
        public async Task<IActionResult> MyStudents(CancellationToken ct)
            => R(await _service.MyStudentsAsync(ct));

        [HttpGet("performance/class/{classId}")]
        [Authorize(Policy = Policies.TenantStaff)]
        public async Task<IActionResult> ClassPerformance(string classId, CancellationToken ct)
            => R(await _service.ClassPerformanceAsync(classId, ct));

        [HttpGet("performance/subject/{subjectId}")]
        [Authorize(Policy = Policies.TenantStaff)]
        public async Task<IActionResult> SubjectPerformance(string subjectId, CancellationToken ct)
            => R(await _service.SubjectPerformanceAsync(subjectId, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
