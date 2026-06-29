using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.EngagementDto;
using DerasaX.Domain.Common;

namespace DerasaX.Application.Services.Abstractions.Engagement
{
    public interface ICommunityService
    {
        Task<PaginationResponse<IEnumerable<CommunityDto>>> ListAsync(CommunityParameters p, CancellationToken ct = default);
        Task<ApiResponse<CommunityDto>> GetAsync(string id, CancellationToken ct = default);
        Task<ApiResponse<CommunityDto>> CreateAsync(CreateCommunityDto dto, CancellationToken ct = default);
        Task<ApiResponse<CommunityDto>> UpdateAsync(string id, UpdateCommunityDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> ArchiveAsync(string id, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<CommunityMemberDto>>> MembersAsync(string id, CancellationToken ct = default);
        Task<ApiResponse<bool>> JoinAsync(string id, CancellationToken ct = default);
        Task<ApiResponse<bool>> LeaveAsync(string id, CancellationToken ct = default);
        Task<ApiResponse<CommunityMemberDto>> AddMemberAsync(string id, AddMemberDto dto, CancellationToken ct = default);
        Task<ApiResponse<PostDto>> CreatePostAsync(string id, CreatePostDto dto, CancellationToken ct = default);
        Task<PaginationResponse<IEnumerable<PostDto>>> ListPostsAsync(string id, CommunityParameters p, CancellationToken ct = default);
        Task<ApiResponse<bool>> DeletePostAsync(string postId, CancellationToken ct = default);
        Task<ApiResponse<CommentDto>> CommentAsync(string postId, CreateCommentDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> DeleteCommentAsync(string commentId, CancellationToken ct = default);
        Task<ApiResponse<bool>> ReportPostAsync(string postId, ReportPostDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> ModeratePostAsync(string postId, ModeratePostDto dto, CancellationToken ct = default);
    }

    public interface ICompetitionService
    {
        Task<PaginationResponse<IEnumerable<CompetitionDto>>> ListAsync(CompetitionParameters p, CancellationToken ct = default);
        Task<ApiResponse<CompetitionDto>> GetAsync(string id, CancellationToken ct = default);
        Task<ApiResponse<CompetitionDto>> CreateAsync(CreateCompetitionDto dto, CancellationToken ct = default);
        Task<ApiResponse<CompetitionDto>> UpdateAsync(string id, UpdateCompetitionDto dto, CancellationToken ct = default);
        Task<ApiResponse<CompetitionDto>> PublishAsync(string id, CancellationToken ct = default);
        Task<ApiResponse<CompetitionDto>> ArchiveAsync(string id, CancellationToken ct = default);
        Task<ApiResponse<CompetitionEntryDto>> EnterAsync(string id, CancellationToken ct = default);
        Task<ApiResponse<bool>> RecordScoreAsync(string id, string entryId, RecordScoreDto dto, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<LeaderboardRowDto>>> LeaderboardAsync(string id, CancellationToken ct = default);
        /// <summary>Phase 14 — close a competition, publish results, idempotently award gamification rewards, and notify entrants.</summary>
        Task<ApiResponse<CompetitionDto>> CloseAsync(string id, CancellationToken ct = default);
        /// <summary>Phase 14 (closure) — the entered student submits/updates durable work while the competition is open.</summary>
        Task<ApiResponse<CompetitionSubmissionDto>> SubmitAsync(string id, SubmitCompetitionDto dto, CancellationToken ct = default);
        /// <summary>Phase 14 (closure) — the student reads their own submission for a competition.</summary>
        Task<ApiResponse<CompetitionSubmissionDto>> MySubmissionAsync(string id, CancellationToken ct = default);
        /// <summary>Phase 14 (closure) — staff list all submissions for a competition (to judge/score).</summary>
        Task<ApiResponse<IEnumerable<CompetitionSubmissionDto>>> SubmissionsAsync(string id, CancellationToken ct = default);
    }

    public interface IBadgeService
    {
        Task<ApiResponse<IEnumerable<BadgeDto>>> CatalogAsync(CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<StudentBadgeDto>>> StudentBadgesAsync(string studentId, CancellationToken ct = default);
        Task<ApiResponse<StudentBadgeDto>> AwardAsync(string studentId, AwardBadgeDto dto, CancellationToken ct = default);
        Task<ApiResponse<StudentStreakDto>> StreakAsync(string studentId, CancellationToken ct = default);
        Task<ApiResponse<StudentStreakDto>> UpdateStreakAsync(string studentId, UpdateStreakDto dto, CancellationToken ct = default);
    }

    public interface IOfficeHourService
    {
        Task<ApiResponse<OfficeHourDto>> CreateAsync(CreateOfficeHourDto dto, CancellationToken ct = default);
        Task<ApiResponse<OfficeHourDto>> UpdateAsync(string id, UpdateOfficeHourDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> CancelAsync(string id, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<OfficeHourDto>>> MySessionsAsync(CancellationToken ct = default);
        /// <summary>Role-aware "mine": a student's booked sessions, or a teacher's owned sessions.</summary>
        Task<ApiResponse<IEnumerable<OfficeHourDto>>> MineAsync(CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<OfficeHourDto>>> AvailableAsync(CancellationToken ct = default);
        Task<ApiResponse<BookingDto>> BookAsync(string id, BookOfficeHourDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> CancelBookingAsync(string bookingId, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<BookingDto>>> SessionBookingsAsync(string id, CancellationToken ct = default);
        /// <summary>Phase 14 — the owning teacher (or SchoolAdmin) marks a booking Attended/NoShow; attendance awards gamification points idempotently.</summary>
        Task<ApiResponse<BookingDto>> MarkAttendanceAsync(string bookingId, MarkAttendanceDto dto, CancellationToken ct = default);
    }
}
