namespace DerasaX.Domain.Enums
{
    /// <summary>
    /// Honest per-channel delivery state for a notification. Phase 13 persists the in-app
    /// notification durably (the row IS the in-app delivery), so the in-app channel is always
    /// <see cref="Delivered"/> once committed. The e-mail channel has no configured sender/outbox in
    /// this environment, so it is always <see cref="NotConfigured"/> — never faked as "sent".
    /// </summary>
    public enum NotificationChannelStatus
    {
        /// <summary>No sender/outbox is configured for this channel; delivery was not attempted.</summary>
        NotConfigured = 0,
        /// <summary>Queued/awaiting delivery (reserved for when a real sender exists).</summary>
        Pending = 1,
        /// <summary>Durably delivered (in-app: persisted; other channels: confirmed by the sender).</summary>
        Delivered = 2,
        /// <summary>Delivery was attempted and failed (reserved for when a real sender exists).</summary>
        Failed = 3,
    }
}
