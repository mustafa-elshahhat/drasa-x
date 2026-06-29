using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.OperationsDto;
using DerasaX.Application.Services.Abstractions.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 5 §14 (Increment 7) — tenant support requests. Any tenant member raises and tracks
    /// their own requests; SchoolAdmin sees the whole tenant and responds/drives status. Audited.
    /// </summary>
    [ApiController]
    [Route("api/v1/support-requests")]
    [Authorize(Policy = Policies.TenantMember)]
    public class SupportRequestsController : ControllerBase
    {
        private readonly ISupportService _service;
        public SupportRequestsController(ISupportService service) => _service = service;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSupportRequestDto dto, CancellationToken ct) => R(await _service.CreateAsync(dto, ct));

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] SupportParameters p, CancellationToken ct) => R(await _service.ListAsync(p, ct));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id, CancellationToken ct) => R(await _service.GetAsync(id, ct));

        [HttpPost("{id}/respond")]
        [Authorize(Policy = Policies.SchoolAdminOnly)]
        public async Task<IActionResult> Respond(string id, [FromBody] RespondSupportDto dto, CancellationToken ct) => R(await _service.RespondAsync(id, dto, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
