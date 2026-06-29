using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.SchoolAdminDto;
using DerasaX.Application.Services.Abstractions.SchoolAdminPortal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 11 — School Admin Portal surface. Adds the admin contracts that did not exist before
    /// (aggregate tenant dashboard, parent↔student relationship management, teacher↔class assignment
    /// management). Every other admin page reuses the existing Phase 5 contracts. Strictly
    /// tenant-scoped (SchoolAdminOnly): the tenant is resolved from the trusted token, cross-tenant
    /// ids resolve to 404, and a wrong role / unauthenticated caller is rejected by the policy.
    /// </summary>
    [ApiController]
    [Route("api/v1/school-admin")]
    [Authorize(Policy = Policies.SchoolAdminOnly)]
    public class SchoolAdminController : ControllerBase
    {
        private readonly ISchoolAdminPortalService _service;
        public SchoolAdminController(ISchoolAdminPortalService service) => _service = service;

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard(CancellationToken ct)
            => R(await _service.DashboardAsync(ct));

        // ---- Parent ↔ student relationships ----

        [HttpGet("relationships")]
        public async Task<IActionResult> Relationships([FromQuery] RelationshipParameters p, CancellationToken ct)
            => R(await _service.ListRelationshipsAsync(p, ct));

        [HttpPost("relationships")]
        public async Task<IActionResult> CreateRelationship([FromBody] CreateRelationshipDto dto, CancellationToken ct)
            => R(await _service.CreateRelationshipAsync(dto, ct));

        [HttpPost("relationships/{id}/deactivate")]
        public async Task<IActionResult> DeactivateRelationship(string id, CancellationToken ct)
            => R(await _service.DeactivateRelationshipAsync(id, ct));

        // ---- Teacher ↔ class assignments ----

        [HttpGet("teacher-class-assignments")]
        public async Task<IActionResult> ClassAssignments([FromQuery] TeacherClassAssignmentParameters p, CancellationToken ct)
            => R(await _service.ListClassAssignmentsAsync(p, ct));

        [HttpPost("teacher-class-assignments")]
        public async Task<IActionResult> CreateClassAssignment([FromBody] CreateTeacherClassAssignmentDto dto, CancellationToken ct)
            => R(await _service.CreateClassAssignmentAsync(dto, ct));

        [HttpPost("teacher-class-assignments/{id}/deactivate")]
        public async Task<IActionResult> DeactivateClassAssignment(string id, CancellationToken ct)
            => R(await _service.DeactivateClassAssignmentAsync(id, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
