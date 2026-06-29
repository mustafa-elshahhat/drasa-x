using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;

namespace DerasaX.Application.Common
{
    /// <summary>
    /// Phase 13 — the single, preference-aware place that stages an in-app notification onto the current
    /// unit of work. Every notification emitter (the service-base helpers, support, platform admin and
    /// the notification service) routes through here so that: (1) optional categories honour the
    /// recipient's per-user in-app preference (mandatory categories are never suppressed), and (2) the
    /// honest delivery-state fields are set consistently. The staged row participates in the caller's
    /// transaction; real-time push happens after commit via <c>NotificationRealtimeInterceptor</c>.
    /// </summary>
    public static class NotificationStaging
    {
        /// <summary>
        /// Stages a notification for <paramref name="userId"/> unless the recipient has disabled the
        /// (optional) category in-app. Returns the staged entity, or <c>null</c> when suppressed by
        /// preference (so callers can count what was actually delivered).
        /// </summary>
        public static async Task<Notification?> StageAsync(
            IUnitOfWork uow,
            string tenantId,
            string userId,
            string title,
            string body,
            NotificationCategory category,
            NotificationType type = NotificationType.User,
            string? actionUrl = null,
            string? actorUserId = null,
            string? metadataJson = null,
            CancellationToken ct = default)
        {
            if (!NotificationPolicy.IsMandatory(category))
            {
                var prefs = await uow.Repository<NotificationPreference, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<NotificationPreference, string>(
                        p => p.UserId == userId && p.Category == category));
                var pref = prefs.FirstOrDefault();
                if (pref is not null && !pref.InAppEnabled)
                    return null; // optional category suppressed by the recipient's preference
            }

            var notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                UserId = userId,
                Title = title,
                Body = body,
                ActionUrl = actionUrl,
                NotificationCategory = category,
                NotificationType = type,
                ActorUserId = actorUserId,
                MetadataJson = metadataJson,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                DeliveryStatus = NotificationChannelStatus.Delivered,   // in-app: the committed row IS the delivery
                EmailStatus = NotificationChannelStatus.NotConfigured,  // e-mail: no sender configured (never faked)
            };

            await uow.Repository<Notification, string>().AddAsync(notification);
            return notification;
        }
    }
}
