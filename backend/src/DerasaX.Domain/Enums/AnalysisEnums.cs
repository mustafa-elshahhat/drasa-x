namespace DerasaX.Domain.Enums
{
    /// <summary>Human-review state for an AI-generated analysis output (Phase 6 §13).</summary>
    public enum HumanReviewStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }

    /// <summary>
    /// Pedagogical follow-up level for a pain point (Phase 6 §13). This is a request
    /// for HUMAN review/attention only — never an automatic or punitive action.
    /// </summary>
    public enum EscalationLevel
    {
        None = 0,
        Monitor = 1,
        Escalate = 2
    }
}
