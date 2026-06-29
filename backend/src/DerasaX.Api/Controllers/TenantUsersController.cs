using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.ProvisioningDto;
using DerasaX.Application.Services.Abstractions.Provisioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 5 — SchoolAdmin provisioning and lifecycle management of tenant Student/Teacher/Parent
    /// accounts and their one-time credentials. Strictly tenant-scoped (SchoolAdminOnly); admin
    /// accounts cannot be created here, and cross-tenant ids resolve to 404.
    /// </summary>
    [ApiController]
    [Route("api/v1/tenant-users")]
    [Authorize(Policy = Policies.SchoolAdminOnly)]
    public class TenantUsersController : ControllerBase
    {
        private readonly IUserProvisioningService _service;
        public TenantUsersController(IUserProvisioningService service) => _service = service;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTenantUserDto dto, CancellationToken ct)
            => R(await _service.CreateAsync(dto, ct));

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] TenantUserParameters p, CancellationToken ct)
        {
            var r = await _service.ListAsync(p, ct);
            return StatusCode(r.StatusCode, r);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id, CancellationToken ct)
            => R(await _service.GetAsync(id, ct));

        [HttpPost("{id}/enable")]
        public async Task<IActionResult> Enable(string id, CancellationToken ct)
            => R(await _service.SetEnabledAsync(id, enabled: true, ct));

        [HttpPost("{id}/disable")]
        public async Task<IActionResult> Disable(string id, CancellationToken ct)
            => R(await _service.SetEnabledAsync(id, enabled: false, ct));

        [HttpPost("{id}/reset-credential")]
        public async Task<IActionResult> ResetCredential(string id, CancellationToken ct)
            => R(await _service.ResetCredentialAsync(id, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
