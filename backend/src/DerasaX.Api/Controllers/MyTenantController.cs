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
    /// Phase 5 §14 (Increment 7) — SchoolAdmin self-service over the caller's OWN tenant only:
    /// profile, current subscription, current usage and renewal requests. Cannot read or mutate
    /// any other tenant (strict separation from the SystemAdmin <c>/api/v1/tenants</c> routes).
    /// </summary>
    [ApiController]
    [Route("api/v1/my-tenant")]
    [Authorize(Policy = Policies.SchoolAdminOnly)]
    public class MyTenantController : ControllerBase
    {
        private readonly ITenantSelfService _service;
        public MyTenantController(ITenantSelfService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Current(CancellationToken ct) => R(await _service.CurrentTenantAsync(ct));

        [HttpGet("subscription")]
        public async Task<IActionResult> Subscription(CancellationToken ct) => R(await _service.CurrentSubscriptionAsync(ct));

        [HttpGet("usage")]
        public async Task<IActionResult> Usage(CancellationToken ct) => R(await _service.CurrentUsageAsync(ct));

        [HttpPost("renewal-requests")]
        public async Task<IActionResult> RequestRenewal([FromBody] RequestRenewalDto dto, CancellationToken ct) => R(await _service.RequestRenewalAsync(dto, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
