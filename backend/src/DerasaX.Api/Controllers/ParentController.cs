using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.ProgressDto;
using DerasaX.Application.Services.Abstractions.ParentPortal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 10 — Parent Portal summary + relationship-scoped reads. Class-level
    /// policy is <c>ParentOnly</c> (role gate); the service layer then enforces the
    /// parent's ACTIVE, progress-permitted parent-student links and tenant on every
    /// read, so a parent only ever sees children explicitly linked to them. Deeper
    /// per-child reads (lessons, attempts, insights, grades, recommendations) reuse
    /// the existing relationship-authorized <c>api/v1/students/{studentId}/...</c>
    /// endpoints. No AI runs on read.
    /// </summary>
    [ApiController]
    [Route("api/v1/parent")]
    [Authorize(Policy = Policies.ParentOnly)]
    public class ParentController : ControllerBase
    {
        private readonly IParentPortalService _service;
        public ParentController(IParentPortalService service) => _service = service;

        /// <summary>Parent dashboard summary (count of linked, progress-permitted children).</summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard(CancellationToken ct)
            => R(await _service.DashboardAsync(ct));

        /// <summary>Children the parent is linked to, with academic status snapshots.</summary>
        [HttpGet("children")]
        public async Task<IActionResult> Children(CancellationToken ct)
            => R(await _service.ChildrenAsync(ct));

        /// <summary>Detailed overview of one linked child (403 if unlinked, 404 cross-tenant/unknown).</summary>
        [HttpGet("children/{childId}")]
        public async Task<IActionResult> ChildOverview(string childId, CancellationToken ct)
            => R(await _service.ChildOverviewAsync(childId, ct));

        /// <summary>Read-only attendance for one linked child (403 if unlinked, 404 cross-tenant/unknown).</summary>
        [HttpGet("children/{childId}/attendance")]
        public async Task<IActionResult> ChildAttendance(string childId, [FromQuery] ProgressParameters p, CancellationToken ct)
            => R(await _service.ChildAttendanceAsync(childId, p, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
