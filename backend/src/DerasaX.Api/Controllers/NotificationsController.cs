using DerasaX.Application.Services.Abstractions.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DerasaX.Api.Controllers
{
    // Canonical Phase 5 route is /api/v1/notifications; the original /api/notifications
    // route is retained (non-breaking) for existing clients during convergence.
    [Route("api/v1/notifications")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // The trusted user id is the signed "uid" claim (the rest of the app reads the same).
        // ClaimTypes.NameIdentifier is unreliable here because default inbound claim mapping can
        // surface the "sub" (username) claim under the same type ahead of the explicit user id.
        private string? CurrentUserId() =>
            User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        // GET /api/notifications?page=1&pageSize=20
        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var userId = CurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var notifications = await _notificationService.GetNotificationsAsync(userId, page, pageSize);
            return Ok(notifications);
        }

        // GET /api/notifications/unread-count
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = CurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new { unreadCount = count });
        }

        // PATCH /api/notifications/{id}/read
        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            var userId = CurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var success = await _notificationService.MarkAsReadAsync(id, userId);
            if (!success) return NotFound();

            return NoContent();
        }

        // PATCH /api/notifications/read-all
        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = CurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _notificationService.MarkAllReadAsync(userId);
            return NoContent();
        }
    }
}
