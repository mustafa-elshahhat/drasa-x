namespace DerasaX.Domain.Enums
{
    public enum CommunityVisibility { Public = 0, TenantOnly = 1, ClassOnly = 2 }
    public enum CommunityMemberRole { Member = 0, Moderator = 1, Owner = 2 }
    public enum ReportStatus { Open = 0, Reviewed = 1, Dismissed = 2, ActionTaken = 3 }
    public enum CompetitionStatus { Draft = 0, Published = 1, Active = 2, Closed = 3, Archived = 4 }
    public enum BadgeType { Achievement = 0, Streak = 1, Competition = 2, Participation = 3 }
    public enum OfficeHourStatus { Scheduled = 0, Cancelled = 1, Completed = 2 }
    public enum OfficeHourBookingStatus { Requested = 0, Confirmed = 1, Cancelled = 2, Attended = 3, NoShow = 4 }

    /// <summary>
    /// Phase 14 — the originating workflow for a ledger point transaction. Source +
    /// source id + idempotency key together prevent the same real-world event from ever
    /// awarding points twice.
    /// </summary>
    public enum PointSourceType
    {
        ManualAward = 0,
        CompetitionReward = 1,
        OfficeHourAttendance = 2,
        CommunityActivity = 3,
        BadgeAward = 4,
        StreakMilestone = 5,
        Other = 6
    }

    /// <summary>
    /// Phase 14 — the deterministic trigger a <c>GamificationRule</c> responds to. Rules
    /// make the point value of automatic awards tenant-configurable while the award itself
    /// remains code-driven and idempotent.
    /// </summary>
    public enum GamificationTrigger
    {
        OfficeHourAttended = 0,
        CompetitionTopRank = 1,
        CompetitionParticipation = 2,
        CommunityPost = 3,
        ManualAward = 4
    }
}
