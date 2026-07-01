using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.OperationsDto;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 5 §14 (Increment 7) — SystemAdmin (platform) administration of tenants and
    /// subscriptions. Tenant status changes integrate with the Phase 3 login/tenant gate. Strictly
    /// separated from SchoolAdmin self-service (<c>/api/v1/my-tenant</c>).
    /// </summary>
    [ApiController]
    [Route("api/v1/tenants")]
    [Authorize(Policy = Policies.SystemAdminOnly)]
    public class TenantsController : ControllerBase
    {
        private readonly ITenantAdminService _service;
        public TenantsController(ITenantAdminService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] TenantParameters p, CancellationToken ct) => R(await _service.ListTenantsAsync(p, ct));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id, CancellationToken ct) => R(await _service.GetTenantAsync(id, ct));

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTenantDto dto, CancellationToken ct) => R(await _service.CreateTenantAsync(dto, ct));

        [HttpPost("{id}/activate")]
        public async Task<IActionResult> Activate(string id, CancellationToken ct) => R(await _service.SetStatusAsync(id, TenantStatus.Active, ct));

        [HttpPost("{id}/suspend")]
        public async Task<IActionResult> Suspend(string id, CancellationToken ct) => R(await _service.SetStatusAsync(id, TenantStatus.Suspended, ct));

        [HttpPost("{id}/reactivate")]
        public async Task<IActionResult> Reactivate(string id, CancellationToken ct) => R(await _service.SetStatusAsync(id, TenantStatus.Active, ct));

        [HttpPost("{id}/archive")]
        public async Task<IActionResult> Archive(string id, CancellationToken ct) => R(await _service.SetStatusAsync(id, TenantStatus.Archived, ct));

        [HttpGet("plans")]
        public async Task<IActionResult> Plans(CancellationToken ct) => R(await _service.ListPlansAsync(ct));

        [HttpPost("plans")]
        public async Task<IActionResult> CreatePlan([FromBody] CreatePlanDto dto, CancellationToken ct) => R(await _service.CreatePlanAsync(dto, ct));

        [HttpPut("plans/{id}")]
        public async Task<IActionResult> UpdatePlan(string id, [FromBody] UpdatePlanDto dto, CancellationToken ct) => R(await _service.UpdatePlanAsync(id, dto, ct));

        [HttpPost("subscriptions")]
        public async Task<IActionResult> AssignPlan([FromBody] AssignPlanDto dto, CancellationToken ct) => R(await _service.AssignPlanAsync(dto, ct));

        [HttpGet("{id}/subscription")]
        public async Task<IActionResult> Subscription(string id, CancellationToken ct) => R(await _service.GetSubscriptionAsync(id, ct));

        [HttpPost("renewals/{renewalId}/process")]
        public async Task<IActionResult> ProcessRenewal(string renewalId, [FromBody] ProcessRenewalDto dto, CancellationToken ct) => R(await _service.ProcessRenewalAsync(renewalId, dto, ct));

        [HttpGet("{id}/usage")]
        public async Task<IActionResult> Usage(string id, CancellationToken ct) => R(await _service.TenantUsageAsync(id, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
