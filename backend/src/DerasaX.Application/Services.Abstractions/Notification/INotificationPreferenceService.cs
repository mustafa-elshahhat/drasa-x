using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.NotificationDto;
using DerasaX.Domain.Common;

namespace DerasaX.Application.Services.Abstractions.Notification
{
    /// <summary>
    /// Phase 13 — per-user notification preferences. Returns every category with the user's effective
    /// in-app/e-mail settings (filling defaults for categories the user has never touched), and lets the
    /// user toggle optional categories. Mandatory categories cannot be disabled. Enforced by the routing
    /// path (<c>NotificationStaging</c>) so suppressing an optional category really stops its in-app
    /// notifications.
    /// </summary>
    public interface INotificationPreferenceService
    {
        Task<ApiResponse<IEnumerable<NotificationPreferenceItemDto>>> GetMineAsync(CancellationToken ct = default);
        Task<ApiResponse<NotificationPreferenceItemDto>> UpdateMineAsync(UpdateNotificationPreferenceDto dto, CancellationToken ct = default);
    }
}
