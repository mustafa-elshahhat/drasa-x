using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.OperationsDto;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Api.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 5 §14 (Increment 7) — bounded, date-validated tenant reports backed by real
    /// authoritative data. SchoolAdmin only; no metric is fabricated when its source does not exist.
    /// </summary>
    [ApiController]
    [Route("api/v1/reports")]
    [Authorize(Policy = Policies.SchoolAdminOnly)]
    [EnableRateLimiting(RateLimitPolicies.Reports)]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _service;
        public ReportsController(IReportService service) => _service = service;

        [HttpGet("tenant-users")]
        public async Task<IActionResult> TenantUsers(CancellationToken ct) => R(await _service.TenantUsersAsync(ct));

        [HttpGet("assessment-summary")]
        public async Task<IActionResult> AssessmentSummary([FromQuery] ReportParameters p, CancellationToken ct) => R(await _service.AssessmentSummaryAsync(p, ct));

        [HttpGet("audit-activity")]
        public async Task<IActionResult> AuditActivity([FromQuery] ReportParameters p, CancellationToken ct) => R(await _service.AuditActivityAsync(p, ct));

        [HttpGet("ai-usage-activity")]
        public async Task<IActionResult> AiUsageActivity([FromQuery] ReportParameters p, CancellationToken ct) => R(await _service.AiUsageActivityAsync(p, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
