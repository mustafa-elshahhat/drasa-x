using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.ProgressDto;
using DerasaX.Domain.Common;

namespace DerasaX.Application.Services.Abstractions.Progress
{
    /// <summary>Per-student progress and insight reads. Every method is relationship-authorized.</summary>
    public interface IStudentProgressService
    {
        Task<PaginationResponse<IEnumerable<LessonProgressDto>>> LessonProgressAsync(string studentId, ProgressParameters p, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<SubjectProgressDto>>> SubjectProgressAsync(string studentId, CancellationToken ct = default);
        Task<ApiResponse<ProgressSummaryDto>> SummaryAsync(string studentId, CancellationToken ct = default);
        Task<PaginationResponse<IEnumerable<MetricHistoryDto>>> MetricHistoryAsync(string studentId, MetricHistoryParameters p, CancellationToken ct = default);
        Task<PaginationResponse<IEnumerable<AttemptHistoryDto>>> AttemptHistoryAsync(string studentId, ProgressParameters p, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<StudentInsightDto>>> InsightsAsync(string studentId, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<PainPointDto>>> PainPointsAsync(string studentId, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<RecommendationDto>>> RecommendationsAsync(string studentId, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<PredictionDto>>> PredictionsAsync(string studentId, CancellationToken ct = default);
        Task<ApiResponse<LessonCompletionDto>> CompleteLessonAsync(string lessonId, CancellationToken ct = default);
    }

    public interface IStudentAttendanceService
    {
        Task<ApiResponse<StudentAttendanceDto>> MyAttendanceAsync(ProgressParameters p, CancellationToken ct = default);
    }

    /// <summary>Cross-student dashboards and performance aggregations (server-side, scoped to the caller).</summary>
    public interface IPerformanceService
    {
        Task<ApiResponse<IEnumerable<StudentDashboardRowDto>>> MyStudentsAsync(CancellationToken ct = default);
        Task<ApiResponse<ClassPerformanceDto>> ClassPerformanceAsync(string classId, CancellationToken ct = default);
        Task<ApiResponse<SubjectPerformanceDto>> SubjectPerformanceAsync(string subjectId, CancellationToken ct = default);
    }
}
