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
    /// Phase 5 §14 (Increment 7) — settings and feature flags. SchoolAdmin manages tenant-owned
    /// settings; SystemAdmin manages platform settings and feature flags. Secret values are
    /// returned redacted; all changes are audited; feature evaluation is open to any tenant member.
    /// </summary>
    [ApiController]
    [Route("api/v1")]
    [Authorize] // per-action policies below; SystemAdmin (no tenant) must reach platform settings/flags
    public class SettingsController : ControllerBase
    {
        private readonly ISettingsService _service;
        public SettingsController(ISettingsService service) => _service = service;

        [HttpGet("tenant-settings")]
        [Authorize(Policy = Policies.SchoolAdminOnly)]
        public async Task<IActionResult> TenantSettings(CancellationToken ct) => R(await _service.TenantSettingsAsync(ct));

        [HttpPut("tenant-settings")]
        [Authorize(Policy = Policies.SchoolAdminOnly)]
        public async Task<IActionResult> UpsertTenantSetting([FromBody] UpsertSettingDto dto, CancellationToken ct) => R(await _service.UpsertTenantSettingAsync(dto, ct));

        [HttpGet("system-settings")]
        [Authorize(Policy = Policies.SystemAdminOnly)]
        public async Task<IActionResult> SystemSettings(CancellationToken ct) => R(await _service.SystemSettingsAsync(ct));

        [HttpPut("system-settings")]
        [Authorize(Policy = Policies.SystemAdminOnly)]
        public async Task<IActionResult> UpsertSystemSetting([FromBody] UpsertSettingDto dto, CancellationToken ct) => R(await _service.UpsertSystemSettingAsync(dto, ct));

        [HttpGet("feature-flags")]
        [Authorize(Policy = Policies.SystemAdminOnly)]
        public async Task<IActionResult> FeatureFlags(CancellationToken ct) => R(await _service.FeatureFlagsAsync(ct));

        [HttpPut("feature-flags")]
        [Authorize(Policy = Policies.SystemAdminOnly)]
        public async Task<IActionResult> UpsertFeatureFlag([FromBody] UpsertFeatureFlagDto dto, CancellationToken ct) => R(await _service.UpsertFeatureFlagAsync(dto, ct));

        [HttpGet("feature-flags/{key}/evaluate")]
        [Authorize(Policy = Policies.TenantMember)]
        public async Task<IActionResult> Evaluate(string key, CancellationToken ct) => R(await _service.EvaluateFeatureAsync(key, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
