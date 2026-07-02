using System.Threading;
using System.Threading.Tasks;
using DerasaX.Api.Observability;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.OperationsDto;
using DerasaX.Application.Dto.SystemAdminDto;
using DerasaX.Application.Services.Abstractions.SystemAdminPortal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 12 — System Admin (platform) Portal surface. Adds ONLY the genuinely-missing platform
    /// contracts (aggregate dashboard, platform usage/AI/storage roll-ups, cross-tenant support inbox,
    /// durable platform announcements, create-initial-school-admin, operational status, and the SAFE
    /// non-destructive tenant data export/deletion request). Tenant lifecycle, plans, platform audit,
    /// system settings and feature flags REUSE the existing Phase 5 §14 SystemAdmin routes
    /// (<c>api/v1/tenants</c>, <c>api/v1/platform-audit</c>, <c>api/v1/system-settings</c>,
    /// <c>api/v1/feature-flags</c>). Strictly platform-scope: every endpoint is SystemAdminOnly; a
    /// tenant-scoped role (Student/Teacher/Parent/SchoolAdmin) or an unauthenticated caller is rejected
    /// by the policy.
    /// </summary>
    [ApiController]
    [Route("api/v1/system-admin")]
    [Authorize(Policy = Policies.SystemAdminOnly)]
    public class SystemAdminController : ControllerBase
    {
        private readonly ISystemAdminPortalService _service;
        private readonly HealthCheckService _health;
        private readonly IRuntimeMetrics _metrics;
        private readonly IWebHostEnvironment _env;

        public SystemAdminController(
            ISystemAdminPortalService service,
            HealthCheckService health,
            IRuntimeMetrics metrics,
            IWebHostEnvironment env)
        {
            _service = service;
            _health = health;
            _metrics = metrics;
            _env = env;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard(CancellationToken ct) => R(await _service.DashboardAsync(ct));

        [HttpGet("usage")]
        public async Task<IActionResult> Usage(CancellationToken ct) => R(await _service.PlatformUsageAsync(ct));

        [HttpGet("ai-usage")]
        public async Task<IActionResult> AiUsage(CancellationToken ct) => R(await _service.PlatformAiUsageAsync(ct));

        [HttpGet("storage")]
        public async Task<IActionResult> Storage(CancellationToken ct) => R(await _service.PlatformStorageAsync(ct));

        [HttpGet("subscriptions")]
        public async Task<IActionResult> Subscriptions(CancellationToken ct) => R(await _service.ListSubscriptionsAsync(ct));

        // ---- Cross-tenant support inbox ----

        [HttpGet("support-tickets")]
        public async Task<IActionResult> SupportTickets([FromQuery] SystemSupportParameters p, CancellationToken ct)
            => R(await _service.ListSupportTicketsAsync(p, ct));

        [HttpPost("support-tickets/{id}/respond")]
        public async Task<IActionResult> RespondSupportTicket(string id, [FromBody] RespondSupportDto dto, CancellationToken ct)
            => R(await _service.RespondSupportTicketAsync(id, dto, ct));

        // ---- Platform announcements ----

        [HttpGet("announcements")]
        public async Task<IActionResult> Announcements(CancellationToken ct) => R(await _service.ListAnnouncementsAsync(ct));

        [HttpPost("announcements")]
        public async Task<IActionResult> CreateAnnouncement([FromBody] CreatePlatformAnnouncementDto dto, CancellationToken ct)
            => R(await _service.CreateAnnouncementAsync(dto, ct));

        // ---- Onboarding: initial school admin ----

        [HttpPost("tenants/{id}/school-admins")]
        public async Task<IActionResult> CreateSchoolAdmin(string id, [FromBody] CreateSchoolAdminDto dto, CancellationToken ct)
            => R(await _service.CreateSchoolAdminAsync(id, dto, ct));

        [HttpPost("tenants/{id}/school-admins/{userId}/reset-credential")]
        public async Task<IActionResult> ResetSchoolAdminCredential(string id, string userId, CancellationToken ct)
            => R(await _service.ResetSchoolAdminCredentialAsync(id, userId, ct));

        // ---- Operational posture ----

        [HttpGet("operational-status")]
        public async Task<IActionResult> OperationalStatus(CancellationToken ct)
        {
            var resp = await _service.OperationalStatusAsync(ct);
            // Phase 19 — enrich the (SystemAdmin-only) operational posture with the live
            // health-check report + runtime metrics + deployment identity. Composed here in
            // the API layer so the Application service stays decoupled from health-check infra.
            if (resp?.Data is not null)
            {
                var report = await _health.CheckHealthAsync(ct);
                resp.Data.Storage = Posture(report, "storage");
                resp.Data.AiService = Posture(report, "ai");
                resp.Data.BackgroundJobs = Posture(report, "background-jobs");

                var m = _metrics.Snapshot();
                resp.Data.Metrics = new RuntimeMetricsDto
                {
                    TotalRequests = m.TotalRequests,
                    Status2xx = m.Status2xx,
                    Status3xx = m.Status3xx,
                    Status4xx = m.Status4xx,
                    Status5xx = m.Status5xx,
                    AvgLatencyMs = m.AvgLatencyMs
                };
                resp.Data.UptimeSeconds = m.UptimeSeconds;
                resp.Data.Version = DeploymentInfo.Version;
                resp.Data.Environment = _env.EnvironmentName;
            }
            return R(resp!);
        }

        // Maps a health-check entry to the existing ServicePostureDto shape (status + note).
        private static ServicePostureDto Posture(HealthReport report, string checkName)
        {
            if (report.Entries.TryGetValue(checkName, out var entry))
            {
                return new ServicePostureDto
                {
                    Configured = entry.Status == HealthStatus.Healthy,
                    Status = entry.Status.ToString().ToLowerInvariant(),
                    Note = entry.Description ?? string.Empty
                };
            }
            return new ServicePostureDto { Configured = false, Status = "unknown", Note = "Health check not registered." };
        }

        // ---- SAFE, non-destructive tenant data workflow ----

        [HttpPost("tenants/{id}/data-export")]
        public async Task<IActionResult> ExportTenantData(string id, CancellationToken ct) => R(await _service.ExportTenantDataAsync(id, ct));

        [HttpPost("tenants/{id}/data-deletion-request")]
        public async Task<IActionResult> RequestTenantDeletion(string id, CancellationToken ct) => R(await _service.RequestTenantDeletionAsync(id, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
