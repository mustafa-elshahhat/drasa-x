using System;
using System.Collections.Generic;
using DerasaX.Application.Common;
using DerasaX.Domain.Enums;

namespace DerasaX.Application.Dto.AssessmentDto
{
    /// <summary>Create a draft non-quiz assignment (homework/reading/project/practice).</summary>
    public class CreateHomeworkDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        /// <summary>Homework | Reading | Project | Practice. Quiz is rejected (use the quiz APIs).</summary>
        public string? Type { get; set; }
        public string? SubjectId { get; set; }
        public string? LessonId { get; set; }
        public decimal? MaxScore { get; set; }
        public DateTime? AvailableFrom { get; set; }
        public DateTime? DueDate { get; set; }
    }

    /// <summary>Edit a draft assignment (only permitted while in Draft status).</summary>
    public class UpdateHomeworkDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public decimal? MaxScore { get; set; }
        public DateTime? AvailableFrom { get; set; }
        public DateTime? DueDate { get; set; }
    }

    /// <summary>Publish a draft assignment to a class and/or explicit students.</summary>
    public class PublishHomeworkDto
    {
        public string? SchoolClassId { get; set; }
        public List<string> StudentIds { get; set; } = new();
    }

    public class HomeworkDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public AssignmentType Type { get; set; }
        public AssignmentStatus Status { get; set; }
        public string? SubjectId { get; set; }
        public string? LessonId { get; set; }
        public decimal? MaxScore { get; set; }
        public DateTime? AvailableFrom { get; set; }
        public DateTime? DueDate { get; set; }
        public List<AssignmentTargetDto> Targets { get; set; } = new();
    }

    /// <summary>A homework assignment as seen by a targeted student, with their submission state.</summary>
    public class AssignedHomeworkDto
    {
        public string AssignmentId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public AssignmentType Type { get; set; }
        public DateTime? AvailableFrom { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal? MaxScore { get; set; }
        public bool HasSubmitted { get; set; }
        public string? SubmissionId { get; set; }
        public SubmissionStatus? SubmissionStatus { get; set; }
        public decimal? Score { get; set; }
        public DateTime? GradedAt { get; set; }
        public string? AttachmentFileId { get; set; }
        /// <summary>True when the student may still submit (open window, not yet submitted).</summary>
        public bool CanSubmit { get; set; }
    }

    public class SubmitHomeworkDto
    {
        public string? Content { get; set; }
        public string? AttachmentFileId { get; set; }
    }

    public class GradeHomeworkDto
    {
        public decimal Score { get; set; }
        public string? Feedback { get; set; }
    }

    public class HomeworkSubmissionDto
    {
        public string Id { get; set; } = string.Empty;
        public string AssignmentId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string? AttachmentFileId { get; set; }
        public SubmissionStatus Status { get; set; }
        public DateTime SubmittedAt { get; set; }
        public decimal? Score { get; set; }
        public string? Feedback { get; set; }
        public DateTime? GradedAt { get; set; }
    }

    public class HomeworkSubmissionParameters : PaginationParameters { }
}
