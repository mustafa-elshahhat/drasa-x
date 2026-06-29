using DerasaX.Domain.Enums;

namespace DerasaX.Application.Dto.NotificationDto
{
    public class NotificationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? ActionUrl { get; set; }
        public NotificationCategory Category { get; set; }
        public NotificationType Type { get; set; } = NotificationType.System;
        public string? MetadataJson { get; set; }
    }

    public class NotificationResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? ActionUrl { get; set; }
        public NotificationCategory Category { get; set; }
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }
        /// <summary>Honest e-mail channel state (NotConfigured in this environment — never faked).</summary>
        public NotificationChannelStatus EmailStatus { get; set; } = NotificationChannelStatus.NotConfigured;
        public string? MetadataJson { get; set; }
    }
}
