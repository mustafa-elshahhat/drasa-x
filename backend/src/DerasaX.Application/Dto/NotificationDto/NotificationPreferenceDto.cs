using DerasaX.Domain.Enums;

namespace DerasaX.Application.Dto.NotificationDto
{
    /// <summary>One notification category and the user's effective in-app/e-mail settings for it.</summary>
    public class NotificationPreferenceItemDto
    {
        public NotificationCategory Category { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool InAppEnabled { get; set; } = true;
        public bool EmailEnabled { get; set; } = false;
        /// <summary>Mandatory categories are always delivered in-app and cannot be disabled.</summary>
        public bool Mandatory { get; set; }
        /// <summary>Whether the e-mail channel is actually configured. It is not in this environment, so
        /// e-mail preferences are recorded but honestly reported as not yet deliverable.</summary>
        public bool EmailConfigured { get; set; } = false;
    }

    public class UpdateNotificationPreferenceDto
    {
        public NotificationCategory Category { get; set; }
        public bool InAppEnabled { get; set; } = true;
        public bool EmailEnabled { get; set; } = false;
    }
}
