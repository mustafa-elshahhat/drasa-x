using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace DerasaX.Domain.Entities.Models
{
    public class Notification : BaseEntity<string>
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? ActionUrl { get; set; }
        public NotificationCategory NotificationCategory { get; set; }
        public NotificationType NotificationType { get; set; } = NotificationType.System;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? MetadataJson { get; set; }

        // Phase 13 — honest delivery/read state (additive).
        /// <summary>When the recipient marked this notification read (null while unread).</summary>
        public DateTime? ReadAt { get; set; }
        /// <summary>The user who triggered this notification (e.g. the admin who published an
        /// announcement, the teacher who graded). Null for system-originated notifications.</summary>
        public string? ActorUserId { get; set; }
        /// <summary>In-app delivery state. The committed row IS the in-app delivery, so this is
        /// <see cref="NotificationChannelStatus.Delivered"/> once persisted.</summary>
        public NotificationChannelStatus DeliveryStatus { get; set; } = NotificationChannelStatus.Delivered;
        /// <summary>E-mail channel state. No e-mail sender is configured in this environment, so this is
        /// <see cref="NotificationChannelStatus.NotConfigured"/> — never faked as "sent".</summary>
        public NotificationChannelStatus EmailStatus { get; set; } = NotificationChannelStatus.NotConfigured;

        [ForeignKey("User")]
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
    }
}
