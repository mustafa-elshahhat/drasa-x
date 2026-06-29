using System;
using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// A student's submission to a non-quiz <see cref="Assignment"/> (homework/reading/project/practice).
    /// Quiz attempts are modeled separately by <see cref="QuizSubmission"/>; this entity carries the
    /// free-form/text response, optional attachment reference, and the teacher's grade and feedback.
    /// Tenant-owned. One submission per student per assignment.
    /// </summary>
    public class AssignmentSubmission : AuditableEntity<string>
    {
        public string AssignmentId { get; set; } = string.Empty;
        public Assignment? Assignment { get; set; }

        public string StudentId { get; set; } = string.Empty;
        public Student? Student { get; set; }

        /// <summary>Free-form text response (notes, answer, link description).</summary>
        public string? Content { get; set; }

        /// <summary>Optional reference to an uploaded file metadata record (Phase 5 file contract).</summary>
        public string? AttachmentFileId { get; set; }

        public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public decimal? Score { get; set; }
        public string? Feedback { get; set; }
        public DateTime? GradedAt { get; set; }
        public string? GradedByTeacherId { get; set; }
    }
}
