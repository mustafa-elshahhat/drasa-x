using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.NotificationDto;
using DerasaX.Application.Services.Abstractions.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 13 — per-user notification preferences. Any authenticated tenant member manages their own
    /// preferences only (the trusted user id comes from the token, never the body). Mandatory categories
    /// cannot be disabled (→ 400). Honoured by the routing path so disabling an optional category really
    /// stops its in-app notifications.
    /// </summary>
    [Route("api/v1/notification-preferences")]
    [ApiController]
    [Authorize(Policy = Policies.TenantMember)]
    public class NotificationPreferencesController : ControllerBase
    {
        private readonly INotificationPreferenceService _service;
        public NotificationPreferencesController(INotificationPreferenceService service) => _service = service;

        // GET /api/v1/notification-preferences  → every category with the caller's effective settings.
        [HttpGet]
        public async Task<IActionResult> GetMine(CancellationToken ct)
            => R(await _service.GetMineAsync(ct));

        // PUT /api/v1/notification-preferences  → upsert one category's settings for the caller.
        [HttpPut]
        public async Task<IActionResult> UpdateMine([FromBody] UpdateNotificationPreferenceDto dto, CancellationToken ct)
            => R(await _service.UpdateMineAsync(dto, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
