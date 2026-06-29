using System;
using System.Collections.Generic;
using DerasaX.Application.Common;
using DerasaX.Domain.Enums;

namespace DerasaX.Application.Dto.AssessmentDto
{
    // ---- Assignment ----

    public class AssignQuizDto
    {
        /// <summary>Target a whole class; mutually inclusive with StudentIds (at least one target required).</summary>
        public string? SchoolClassId { get; set; }
        /// <summary>Target individual students (must be enrolled / same tenant).</summary>
        public List<string> StudentIds { get; set; } = new();
        public DateTime? AvailableFrom { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class AssignmentDto
    {
        public string Id { get; set; } = string.Empty;
        public string QuizId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public AssignmentStatus Status { get; set; }
        public DateTime? AvailableFrom { get; set; }
        public DateTime? DueDate { get; set; }
        public List<AssignmentTargetDto> Targets { get; set; } = new();
    }

    public class AssignmentTargetDto
    {
        public string Id { get; set; } = string.Empty;
        public AssignmentTargetType TargetType { get; set; }
        public string? SchoolClassId { get; set; }
        public string? StudentId { get; set; }
    }

    /// <summary>A quiz that is assigned to and available for the current student.</summary>
    public class AssignedQuizDto
    {
        public string QuizId { get; set; } = string.Empty;
        public string? Title { get; set; }
        public QuizType Type { get; set; }
        public int TimeLimitMinutes { get; set; }
        public int? MaxAttempts { get; set; }
        public DateTime? AvailableFrom { get; set; }
        public DateTime? DueDate { get; set; }
        public int AttemptsUsed { get; set; }
        public bool CanAttempt { get; set; }
    }

    // ---- Attempts ----

    public class AttemptSummaryDto
    {
        public string Id { get; set; } = string.Empty;
        public string QuizId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public int AttemptNumber { get; set; }
        public SubmissionStatus Status { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int AchievedScore { get; set; }
        public int TotalScore { get; set; }
        public double Percentage { get; set; }
        public bool IsLatestAttempt { get; set; }
    }

    /// <summary>An attempt as the student sees it while taking it (no correct answers).</summary>
    public class AttemptDetailDto : AttemptSummaryDto
    {
        public string? TeacherFeedback { get; set; }
        /// <summary>The quiz's questions rendered for the taker — correct flags are never populated here.</summary>
        public List<QuestionDto> Questions { get; set; } = new();
        public List<AnswerStateDto> Answers { get; set; } = new();
    }

    public class AnswerStateDto
    {
        /// <summary>The persisted SubmissionAnswer id. Required by the teacher grading
        /// contract (<c>POST /api/v1/submissions/{attemptId}/grade</c> references answers by this id).</summary>
        public string Id { get; set; } = string.Empty;
        public string QuestionId { get; set; } = string.Empty;
        public string? SelectedOptionId { get; set; }
        public string? AnswerText { get; set; }
        /// <summary>Populated only after grading / when the result is released.</summary>
        public bool? IsCorrect { get; set; }
        public int? PointsEarned { get; set; }
        public string? Feedback { get; set; }
    }

    public class SaveAnswerDto
    {
        public string QuestionId { get; set; } = string.Empty;
        public string? SelectedOptionId { get; set; }
        public string? AnswerText { get; set; }
    }

    public class SaveAnswersDto
    {
        public List<SaveAnswerDto> Answers { get; set; } = new();
    }

    public class AttemptParameters : PaginationParameters
    {
        public string? StudentId { get; set; }
        public SubmissionStatus? Status { get; set; }
    }

    // ---- Grading & feedback ----

    public class GradeAnswerDto
    {
        public string AnswerId { get; set; } = string.Empty;
        public int PointsEarned { get; set; }
        public bool IsCorrect { get; set; }
        public string? Feedback { get; set; }
    }

    public class ManualGradeDto
    {
        public List<GradeAnswerDto> Grades { get; set; } = new();
    }

    public class FeedbackDto
    {
        public string Feedback { get; set; } = string.Empty;
    }

    // ---- Analytics ----

    public class QuizAnalyticsDto
    {
        public string QuizId { get; set; } = string.Empty;
        public int TotalSubmissions { get; set; }
        public double AverageScorePercentage { get; set; }
        public int TotalPoints { get; set; }
        public List<QuestionAnalyticsDto> Questions { get; set; } = new();
    }

    public class QuestionAnalyticsDto
    {
        public string QuestionId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int Answered { get; set; }
        public int CorrectCount { get; set; }
        public double CorrectRate { get; set; }
    }
}
