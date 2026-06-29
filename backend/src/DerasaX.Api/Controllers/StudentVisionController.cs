using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions.Vision;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 15 — a student's OWN read-only computer-vision engagement/attendance summary.
    /// Returns only the caller's aggregated data — never other students, never raw images
    /// or biometric artifacts.
    /// </summary>
    [ApiController]
    [Route("api/v1/student/vision")]
    [Authorize(Policy = Policies.StudentOnly)]
    public class StudentVisionController : ControllerBase
    {
        private readonly IClassroomVisionService _service;

        public StudentVisionController(IClassroomVisionService service) => _service = service;

        [HttpGet("engagement-summary")]
        public async Task<IActionResult> MySummary(CancellationToken ct)
        {
            var result = await _service.MyEngagementSummaryAsync(ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
