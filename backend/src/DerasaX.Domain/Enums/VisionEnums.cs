namespace DerasaX.Domain.Enums
{
    /// <summary>Lifecycle of a classroom computer-vision session (Phase 15).</summary>
    public enum VisionSessionStatus
    {
        Active = 0,
        Ended = 1,
        Cancelled = 2
    }

    /// <summary>Which inference backend produced a CV result. Recorded for honesty:
    /// <c>Stub</c> output is a deterministic test/degraded backend and is NEVER treated
    /// as authoritative recognition certainty.</summary>
    public enum VisionEngineKind
    {
        Stub = 0,
        Torch = 1
    }

    /// <summary>Engagement label persisted for analytics. <c>NotReady</c> means the
    /// sequence model has not yet collected enough consecutive frames.</summary>
    public enum EngagementLabel
    {
        NotReady = 0,
        Engaged = 1,
        Disengaged = 2
    }

    /// <summary>Review state of a CV attendance candidate. CV NEVER auto-marks
    /// attendance; an authorized teacher/admin must confirm, reject, or override.</summary>
    public enum CandidateReviewStatus
    {
        Pending = 0,
        Confirmed = 1,
        Rejected = 2,
        Overridden = 3
    }
}
