using System;
using System.Collections.Generic;
using DerasaX.Application.Common;
using DerasaX.Domain.Enums;

namespace DerasaX.Application.Dto.AssessmentDto
{
    // ---- Quiz ----

    /// <summary>Response contract for a quiz (never exposes the entity directly).</summary>
    public class QuizDto
    {
        public string Id { get; set; } = string.Empty;
        public string? Title { get; set; }
        public QuizStatus Status { get; set; }
        public QuizOrigin Origin { get; set; }
        public QuizType Type { get; set; }
        public DifficultyLevel Difficulty { get; set; }
        public int TimeLimitMinutes { get; set; }
        public int? MaxAttempts { get; set; }
        public DateTime? DueDate { get; set; }
        public string? SubjectId { get; set; }
        public string? LessonId { get; set; }
        public int QuestionCount { get; set; }
        public int TotalPoints { get; set; }
        public string? ApprovedByTeacherId { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }

    /// <summary>Full quiz detail including questions. Correct answers are included only when the caller is permitted (teacher/admin).</summary>
    public class QuizDetailDto : QuizDto
    {
        public List<QuestionDto> Questions { get; set; } = new();
    }

    public class AddQuizDto
    {
        public string Title { get; set; } = string.Empty;
        public QuizType Type { get; set; } = QuizType.Lesson;
        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Core;
        public int TimeLimitMinutes { get; set; } = 30;
        public int? MaxAttempts { get; set; }
        public DateTime? DueDate { get; set; }
        /// <summary>Required for a teacher (proves an active subject assignment); optional for SchoolAdmin.</summary>
        public string? SubjectId { get; set; }
        public string? LessonId { get; set; }
    }

    public class UpdateQuizDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public QuizType Type { get; set; } = QuizType.Lesson;
        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Core;
        public int TimeLimitMinutes { get; set; } = 30;
        public int? MaxAttempts { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class QuizParameters : PaginationParameters
    {
        public string? SubjectId { get; set; }
        public QuizStatus? Status { get; set; }
        public QuizType? Type { get; set; }
    }

    // ---- Question ----

    public class QuestionOptionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        /// <summary>Only populated for teacher/admin views — never exposed to a student before policy permits.</summary>
        public bool? IsCorrect { get; set; }
    }

    public class QuestionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public QuestionType Type { get; set; }
        public int Order { get; set; }
        public int Points { get; set; }
        /// <summary>Only populated for teacher/admin views.</summary>
        public string? CorrectAnswerText { get; set; }
        public string? Explanation { get; set; }
        public List<QuestionOptionDto> Options { get; set; } = new();
    }

    public class AddQuestionOptionDto
    {
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }

    public class AddQuestionDto
    {
        public string Text { get; set; } = string.Empty;
        public QuestionType Type { get; set; } = QuestionType.MCQ;
        public int Order { get; set; }
        public int Points { get; set; } = 1;
        public string? CorrectAnswerText { get; set; }
        public string? Explanation { get; set; }
        public List<AddQuestionOptionDto> Options { get; set; } = new();
    }

    public class UpdateQuestionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public QuestionType Type { get; set; } = QuestionType.MCQ;
        public int Order { get; set; }
        public int Points { get; set; } = 1;
        public string? CorrectAnswerText { get; set; }
        public string? Explanation { get; set; }
        public List<AddQuestionOptionDto> Options { get; set; } = new();
    }
}
