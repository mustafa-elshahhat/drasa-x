using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.AcademicDto;
using DerasaX.Application.Services.Abstractions.Academic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    /// <summary>Term/semester administration within an academic year (Phase 5 §9.1).</summary>
    [ApiController]
    [Route("api/v1/terms")]
    [Authorize(Policy = Policies.TenantMember)]
    public class TermsController : ControllerBase
    {
        private readonly ITermService _service;
        public TermsController(ITermService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] TermParameters parameters, CancellationToken ct)
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
        public async Task<IActionResult> Create([FromBody] AddTermDto dto, CancellationToken ct)
        {
            var result = await _service.CreateAsync(dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Policies.SchoolAdminOnly)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateTermDto dto, CancellationToken ct)
        {
            dto.Id = id;
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
