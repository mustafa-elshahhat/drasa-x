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
    /// Student enrollment administration (Phase 5 §9.1). Cross-tenant student
    /// associations are rejected (student membership is validated against the trusted
    /// tenant, with the same-tenant database trigger as backstop).
    /// </summary>
    [ApiController]
    [Route("api/v1/enrollments")]
    [Authorize(Policy = Policies.TenantMember)]
    public class EnrollmentsController : ControllerBase
    {
        private readonly IEnrollmentService _service;
        public EnrollmentsController(IEnrollmentService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] EnrollmentParameters parameters, CancellationToken ct)
        {
            var result = await _service.ListAsync(parameters, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [Authorize(Policy = Policies.SchoolAdminOnly)]
        public async Task<IActionResult> Enroll([FromBody] AddEnrollmentDto dto, CancellationToken ct)
        {
            var result = await _service.EnrollAsync(dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{id}/withdraw")]
        [Authorize(Policy = Policies.SchoolAdminOnly)]
        public async Task<IActionResult> Withdraw(string id, [FromBody] WithdrawEnrollmentDto dto, CancellationToken ct)
        {
            dto.Id = id;
            var result = await _service.WithdrawAsync(dto, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
