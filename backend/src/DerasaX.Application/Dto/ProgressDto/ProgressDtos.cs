using System;
using System.Collections.Generic;
using DerasaX.Application.Common;
using DerasaX.Domain.Enums;

namespace DerasaX.Application.Dto.ProgressDto
{
    public class LessonProgressDto
    {
        public string Id { get; set; } = string.Empty;
        public string LessonId { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public decimal CompletionPercentage { get; set; }
        public int TimeSpentSeconds { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? LastAccessedAt { get; set; }
    }

    public class LessonCompletionDto : LessonProgressDto
    {
        public bool Created { get; set; }
    }

    /// <summary>A single lesson's real detail for the student player, with the caller's progress.
    /// Served by GET /api/v1/student/lessons/{lessonId} so the page never brute-force walks the tree.</summary>
    public class StudentLessonDetailDto
    {
        public string LessonId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string UnitId { get; set; } = string.Empty;
        public string? UnitTitle { get; set; }
        public string SubjectId { get; set; } = string.Empty;
        public string? SubjectName { get; set; }
        public bool IsCompleted { get; set; }
        public decimal CompletionPercentage { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class AttendanceRecordDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime AttendanceDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; }
        public string Source { get; set; } = string.Empty;
        public string SessionKey { get; set; } = string.Empty;
        public string? SchoolClassId { get; set; }
        public string? Notes { get; set; }
    }

    public class AttendanceSummaryDto
    {
        public int Total { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Late { get; set; }
        public int Excused { get; set; }
        public decimal AttendancePercentage { get; set; }
    }

    public class StudentAttendanceDto
    {
        public AttendanceSummaryDto Summary { get; set; } = new();
        public IEnumerable<AttendanceRecordDto> Records { get; set; } = Array.Empty<AttendanceRecordDto>();
    }

    public class SubjectProgressDto
    {
        public string Id { get; set; } = string.Empty;
        public string SubjectId { get; set; } = string.Empty;
        public decimal CompletionPercentage { get; set; }
        public decimal AverageScore { get; set; }
        public int LessonsCompleted { get; set; }
        public int TotalLessons { get; set; }
        public DateTime? LastActivityAt { get; set; }
    }

    public class ProgressSummaryDto
    {
        public string StudentId { get; set; } = string.Empty;
        public int LessonsTracked { get; set; }
        public int LessonsCompleted { get; set; }
        public decimal AverageLessonCompletion { get; set; }
        public int QuizAttempts { get; set; }
        public decimal AverageQuizPercentage { get; set; }
        public int SubjectsTracked { get; set; }
    }

    public class MetricHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public ProgressMetricType MetricType { get; set; }
        public decimal Value { get; set; }
        public DateTime MeasuredAt { get; set; }
        public string? SourceEntityType { get; set; }
        public string? SourceEntityId { get; set; }
    }

    public class AttemptHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string QuizId { get; set; } = string.Empty;
        public int AttemptNumber { get; set; }
        public SubmissionStatus Status { get; set; }
        public int AchievedScore { get; set; }
        public int TotalScore { get; set; }
        public double Percentage { get; set; }
        public DateTime? SubmittedAt { get; set; }
    }

    // ---- AI-derived (stored) records. Always labelled so consumers know provenance. ----

    public class StudentInsightDto
    {
        public string Id { get; set; } = string.Empty;
        public PerformanceLevel Performance { get; set; }
        public decimal ConfidenceScore { get; set; }
        public string? Summary { get; set; }
        public InsightPeriod Period { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public DateTime GeneratedAt { get; set; }
        /// <summary>Provenance label: these are STORED AI outputs, not computed live in Phase 5.</summary>
        public string Source { get; set; } = "stored-ai-output";
    }

    public class PainPointDto
    {
        public string Id { get; set; } = string.Empty;
        public PainPointCategory Category { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal ConfidenceScore { get; set; }
        public bool IsResolved { get; set; }
        public DateTime DetectedAt { get; set; }
        public string Source { get; set; } = "stored-ai-output";
    }

    public class RecommendationDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public RecommendationStatus Status { get; set; }
        public DateTime GeneratedAt { get; set; }
        public DateTime? DueAt { get; set; }
        public string Source { get; set; } = "stored-ai-output";
    }

    public class PredictionDto
    {
        public string Id { get; set; } = string.Empty;
        public PredictionKind Kind { get; set; }
        public decimal PredictedScore { get; set; }
        public PerformanceLevel Level { get; set; }
        public decimal ConfidenceScore { get; set; }
        public string? ModelName { get; set; }
        public string? ModelVersion { get; set; }
        public DateTime PredictedAt { get; set; }
        public string Source { get; set; } = "stored-ai-output";
    }

    // ---- Performance aggregations ----

    public class ClassPerformanceDto
    {
        public string ClassId { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public int QuizAttempts { get; set; }
        public decimal AverageQuizPercentage { get; set; }
    }

    public class SubjectPerformanceDto
    {
        public string SubjectId { get; set; } = string.Empty;
        public int QuizCount { get; set; }
        public int SubmissionCount { get; set; }
        public decimal AverageQuizPercentage { get; set; }
    }

    public class StudentDashboardRowDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int QuizAttempts { get; set; }
        public decimal AverageQuizPercentage { get; set; }
    }

    // ---- Query parameters ----

    public class ProgressParameters : PaginationParameters
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    public class MetricHistoryParameters : ProgressParameters
    {
        public ProgressMetricType? MetricType { get; set; }
    }
}
