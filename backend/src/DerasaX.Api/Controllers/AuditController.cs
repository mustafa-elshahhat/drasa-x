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
    /// Phase 5 §14 (Increment 7) — authorized, paginated, filterable audit queries. SchoolAdmin
    /// reads its OWN tenant's trail (tenant-scoped); SystemAdmin reads the full platform trail.
    /// </summary>
    [ApiController]
    [Route("api/v1")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditQueryService _service;
        public AuditController(IAuditQueryService service) => _service = service;

        [HttpGet("audit")]
        [Authorize(Policy = Policies.SchoolAdminOnly)]
        public async Task<IActionResult> TenantAudit([FromQuery] AuditParameters p, CancellationToken ct)
            => R(await _service.QueryAsync(p, platformScope: false, ct));

        [HttpGet("platform-audit")]
        [Authorize(Policy = Policies.SystemAdminOnly)]
        public async Task<IActionResult> PlatformAudit([FromQuery] AuditParameters p, CancellationToken ct)
            => R(await _service.QueryAsync(p, platformScope: true, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
