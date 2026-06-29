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
    /// Academic-year administration (Phase 5 §9.1). Tenant-scoped: every operation
    /// resolves the tenant from the trusted access-token claim. Reads are open to any
    /// tenant member; writes are restricted to the tenant's SchoolAdmin.
    /// </summary>
    [ApiController]
    [Route("api/v1/academic-years")]
    [Authorize(Policy = Policies.TenantMember)]
    public class AcademicYearsController : ControllerBase
    {
        private readonly IAcademicYearService _service;
        private readonly ILogger<AcademicYearsController> _logger;

        public AcademicYearsController(IAcademicYearService service, ILogger<AcademicYearsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] AcademicYearParameters parameters, CancellationToken ct)
        {
            var result = await _service.ListAsync(parameters, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, CancellationToken ct)
        {
            var result = await _service.GetByIdAsync(id, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [Authorize(Policy = Policies.SchoolAdminOnly)]
        public async Task<IActionResult> Create([FromBody] AddAcademicYearDto dto, CancellationToken ct)
        {
            _logger.LogInformation("Creating academic year");
            var result = await _service.CreateAsync(dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Policies.SchoolAdminOnly)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateAcademicYearDto dto, CancellationToken ct)
        {
            dto.Id = id; // route id is authoritative
            var result = await _service.UpdateAsync(dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = Policies.SchoolAdminOnly)]
        public async Task<IActionResult> Archive(string id, CancellationToken ct)
        {
            var result = await _service.ArchiveAsync(id, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
