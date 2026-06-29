using System;
using DerasaX.Domain.Enums;

namespace DerasaX.Application.Common
{
    /// <summary>
    /// Phase 13 — central policy for notification categories: which are mandatory (essential/security
    /// notices the user can never opt out of) versus optional (suppressible by a per-user preference).
    /// Keeping this in one place lets the routing path, the preference API and the tests agree.
    /// </summary>
    public static class NotificationPolicy
    {
        /// <summary>
        /// Mandatory categories are always delivered regardless of preferences and cannot be disabled
        /// via the preference API. <see cref="NotificationCategory.Warning"/> carries security/account
        /// and other essential warnings, so it is mandatory.
        /// </summary>
        public static bool IsMandatory(NotificationCategory category) =>
            category == NotificationCategory.Warning;

        /// <summary>Every category a user can be shown/express a preference about.</summary>
        public static readonly NotificationCategory[] All =
            (NotificationCategory[])Enum.GetValues(typeof(NotificationCategory));
    }
}
