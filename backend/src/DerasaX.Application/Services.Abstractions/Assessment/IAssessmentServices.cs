using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.AssessmentDto;
using DerasaX.Domain.Common;

namespace DerasaX.Application.Services.Abstractions.Assessment
{
    /// <summary>Teacher/SchoolAdmin quiz authoring + lifecycle (draft → questions → publish → archive).</summary>
    public interface IQuizAuthoringService
    {
        Task<PaginationResponse<IEnumerable<QuizDto>>> ListAsync(QuizParameters parameters, CancellationToken ct = default);
        Task<ApiResponse<QuizDetailDto>> GetByIdAsync(string id, CancellationToken ct = default);
        Task<ApiResponse<QuizDto>> CreateAsync(AddQuizDto dto, CancellationToken ct = default);
        Task<ApiResponse<QuizDto>> UpdateAsync(UpdateQuizDto dto, CancellationToken ct = default);
        Task<ApiResponse<QuestionDto>> AddQuestionAsync(string quizId, AddQuestionDto dto, CancellationToken ct = default);
        Task<ApiResponse<QuestionDto>> UpdateQuestionAsync(string quizId, UpdateQuestionDto dto, CancellationToken ct = default);
        Task<ApiResponse<bool>> DeleteQuestionAsync(string quizId, string questionId, CancellationToken ct = default);
        Task<ApiResponse<QuizDto>> PublishAsync(string id, CancellationToken ct = default);
        Task<ApiResponse<QuizDto>> ArchiveAsync(string id, CancellationToken ct = default);
    }

    /// <summary>Quiz assignment to classes/students and student-facing assigned-quiz listing.</summary>
    public interface IQuizAssignmentService
    {
        Task<ApiResponse<AssignmentDto>> AssignAsync(string quizId, AssignQuizDto dto, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<AssignmentDto>>> ListForQuizAsync(string quizId, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<AssignedQuizDto>>> ListAssignedToMeAsync(CancellationToken ct = default);
    }

    /// <summary>Student attempt lifecycle: start → save answers → submit → history/result.</summary>
    public interface IQuizAttemptService
    {
        Task<ApiResponse<AttemptDetailDto>> StartAsync(string quizId, CancellationToken ct = default);
        Task<ApiResponse<AttemptDetailDto>> GetAsync(string attemptId, CancellationToken ct = default);
        Task<ApiResponse<AttemptDetailDto>> SaveAnswersAsync(string attemptId, SaveAnswersDto dto, CancellationToken ct = default);
        Task<ApiResponse<AttemptSummaryDto>> SubmitAsync(string attemptId, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<AttemptSummaryDto>>> MyHistoryAsync(string quizId, CancellationToken ct = default);
        Task<ApiResponse<AttemptDetailDto>> MyResultAsync(string attemptId, CancellationToken ct = default);
    }

    /// <summary>Teacher submission review, manual grading, feedback and question-level analytics.</summary>
    public interface IQuizGradingService
    {
        Task<PaginationResponse<IEnumerable<AttemptSummaryDto>>> ListSubmissionsAsync(string quizId, AttemptParameters parameters, CancellationToken ct = default);
        Task<ApiResponse<AttemptDetailDto>> GetSubmissionAsync(string attemptId, CancellationToken ct = default);
        Task<ApiResponse<AttemptSummaryDto>> GradeAsync(string attemptId, ManualGradeDto dto, CancellationToken ct = default);
        Task<ApiResponse<AttemptSummaryDto>> FeedbackAsync(string attemptId, FeedbackDto dto, CancellationToken ct = default);
        Task<ApiResponse<QuizAnalyticsDto>> AnalyticsAsync(string quizId, CancellationToken ct = default);
    }
}
