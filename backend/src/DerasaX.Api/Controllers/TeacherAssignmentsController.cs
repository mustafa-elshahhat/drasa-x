using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.AcademicDto;
using DerasaX.Application.Services.Abstractions.Academic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Teacher→subject assignment administration (Phase 5 §9.1). Cross-tenant teacher
    /// associations are rejected (teacher membership validated against the trusted
    /// tenant, with the same-tenant database trigger as backstop).
    /// </summary>
    [ApiController]
    [Route("api/v1/teacher-subject-assignments")]
    [Authorize(Policy = Policies.TenantMember)]
    public class TeacherAssignmentsController : ControllerBase
    {
        private readonly ITeacherAssignmentService _service;
        public TeacherAssignmentsController(ITeacherAssignmentService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] TeacherSubjectAssignmentParameters parameters, CancellationToken ct)
        {
            var result = await _service.ListAsync(parameters, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [Authorize(Policy = Policies.SchoolAdminOnly)]
        public async Task<IActionResult> Assign([FromBody] AddTeacherSubjectAssignmentDto dto, CancellationToken ct)
        {
            var result = await _service.AssignAsync(dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = Policies.SchoolAdminOnly)]
        public async Task<IActionResult> Deactivate(string id, CancellationToken ct)
        {
            var result = await _service.DeactivateAsync(id, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
