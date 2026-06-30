using System;
using System.Collections.Generic;
using DerasaX.Application.Common;
using DerasaX.Domain.Enums;

namespace DerasaX.Application.Dto.EngagementDto
{
    // ---- Communities ----

    public class CreateCommunityDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public CommunityVisibility Visibility { get; set; } = CommunityVisibility.TenantOnly;
        public string? SchoolClassId { get; set; }
        /// <summary>Phase 14 — optional grade-eligibility gate keyed off academic data.</summary>
        public string? EligibleGradeId { get; set; }
    }

    public class UpdateCommunityDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public CommunityVisibility Visibility { get; set; } = CommunityVisibility.TenantOnly;
        /// <summary>Phase 14 — optional grade-eligibility gate keyed off academic data.</summary>
        public string? EligibleGradeId { get; set; }
    }

    public class CommunityDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public CommunityVisibility Visibility { get; set; }
        public string? SchoolClassId { get; set; }
        public string? EligibleGradeId { get; set; }
        public int MemberCount { get; set; }
    }

    public class CommunityMemberDto
    {
        public string UserId { get; set; } = string.Empty;
        public CommunityMemberRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
    }

    public class AddMemberDto
    {
        public string UserId { get; set; } = string.Empty;
        public CommunityMemberRole Role { get; set; } = CommunityMemberRole.Member;
    }

    public class CreatePostDto
    {
        public string Content { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
    }

    public class PostDto
    {
        public string Id { get; set; } = string.Empty;
        public string CommunityId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public int CommentsCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateCommentDto { public string Body { get; set; } = string.Empty; }

    public class CommentDto
    {
        public string Id { get; set; } = string.Empty;
        public string PostId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ReportPostDto { public string Reason { get; set; } = string.Empty; }

    public class ModeratePostDto
    {
        public ReportStatus Status { get; set; }
        public bool RemovePost { get; set; }
    }

    public class CommunityParameters : PaginationParameters { }

    // ---- Competitions ----

    public class CreateCompetitionDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
    }

    public class UpdateCompetitionDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
    }

    public class CompetitionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public CompetitionStatus Status { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
    }

    public class CompetitionEntryDto
    {
        public string Id { get; set; } = string.Empty;
        public string CompetitionId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public DateTime EnteredAt { get; set; }
    }

    public class RecordScoreDto { public decimal Score { get; set; } }

    /// <summary>Phase 14 (closure) — a student submits/updates durable work for a competition.</summary>
    public class SubmitCompetitionDto { public string Content { get; set; } = string.Empty; }

    public class CompetitionSubmissionDto
    {
        public string Id { get; set; } = string.Empty;
        public string CompetitionId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
    }

    public class LeaderboardRowDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string? StudentName { get; set; }
        public decimal Score { get; set; }
        public int Rank { get; set; }
    }

    public class CompetitionParameters : PaginationParameters
    {
        public CompetitionStatus? Status { get; set; }
    }

    // ---- Badges & streaks ----

    public class BadgeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public BadgeType Type { get; set; }
    }

    public class AwardBadgeDto
    {
        public string BadgeId { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    public class StudentBadgeDto
    {
        public string Id { get; set; } = string.Empty;
        public string BadgeId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public DateTime AwardedAt { get; set; }
        public string? AwardedReason { get; set; }
    }

    public class StudentStreakDto
    {
        public string Id { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public int CurrentCount { get; set; }
        public int LongestCount { get; set; }
        public DateTime LastActivityDate { get; set; }
    }

    public class UpdateStreakDto { public DateTime ActivityDate { get; set; } }

    // ---- Office hours ----

    public class CreateOfficeHourDto
    {
        public string Title { get; set; } = string.Empty;
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public int Capacity { get; set; } = 1;
    }

    public class UpdateOfficeHourDto
    {
        public string Title { get; set; } = string.Empty;
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public int Capacity { get; set; }
    }

    public class OfficeHourDto
    {
        public string Id { get; set; } = string.Empty;
        public string TeacherId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public int Capacity { get; set; }
        public int BookedCount { get; set; }
        public OfficeHourStatus Status { get; set; }
    }

    public class BookingDto
    {
        public string Id { get; set; } = string.Empty;
        public string OfficeHourSessionId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public OfficeHourBookingStatus Status { get; set; }
        public DateTime BookedAt { get; set; }
    }

    public class BookOfficeHourDto { public string? Notes { get; set; } }

    public class OfficeHourParameters : PaginationParameters { }

    /// <summary>Phase 14 — teacher/admin marks a booking's attendance after the session.</summary>
    public class MarkAttendanceDto { public OfficeHourBookingStatus Status { get; set; } = OfficeHourBookingStatus.Attended; }

    // ---- Phase 14: gamification points ledger ----

    public class PointTransactionDto
    {
        public string Id { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public int Points { get; set; }
        public string Reason { get; set; } = string.Empty;
        public PointSourceType SourceType { get; set; }
        public string? SourceId { get; set; }
        public DateTime AwardedAt { get; set; }
    }

    public class ManualAwardPointsDto
    {
        public int Points { get; set; }
        public string Reason { get; set; } = string.Empty;
        /// <summary>Optional client-supplied idempotency key (e.g. a UI submission id) to make a retry safe.</summary>
        public string? IdempotencyKey { get; set; }
    }

    public class StudentPointsSummaryDto
    {
        public string StudentId { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        public int TransactionCount { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public int BadgeCount { get; set; }
    }

    public class PointLeaderboardRowDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string? StudentName { get; set; }
        public int TotalPoints { get; set; }
        public int Rank { get; set; }
    }

    public class PointLeaderboardParameters : PaginationParameters
    {
        /// <summary>Optional grade filter; the board is always tenant-scoped.</summary>
        public string? GradeId { get; set; }
    }

    public class PointLedgerParameters : PaginationParameters { }

    public class GamificationRuleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public GamificationTrigger Trigger { get; set; }
        public int Points { get; set; }
        public string? BadgeId { get; set; }
        public bool Enabled { get; set; }
    }

    public class UpsertGamificationRuleDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public GamificationTrigger Trigger { get; set; }
        public int Points { get; set; }
        public string? BadgeId { get; set; }
        public bool Enabled { get; set; } = true;
    }
}
