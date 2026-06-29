using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions.Vision;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 15 — a parent's read-only computer-vision engagement/attendance summary for a
    /// LINKED child only. Access is relationship-authorized (parent ↔ child); a parent can
    /// never see another student, and never raw images or biometric artifacts.
    /// </summary>
    [ApiController]
    [Route("api/v1/parent/vision")]
    [Authorize(Policy = Policies.ParentOnly)]
    public class ParentVisionController : ControllerBase
    {
        private readonly IClassroomVisionService _service;

        public ParentVisionController(IClassroomVisionService service) => _service = service;

        [HttpGet("children/{childId}/engagement-summary")]
        public async Task<IActionResult> ChildSummary(string childId, CancellationToken ct)
        {
            var result = await _service.ChildEngagementSummaryAsync(childId, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
