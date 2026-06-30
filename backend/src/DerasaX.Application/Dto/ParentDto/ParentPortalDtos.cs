using System;
using DerasaX.Application.Dto.ProgressDto;

namespace DerasaX.Application.Dto.ParentDto
{
    // =========================================================================
    // Phase 10 — Parent Portal read models. Every figure is aggregated
    // server-side from authoritative records and scoped to the parent's ACTIVE,
    // progress-permitted parent-student links (ParentStudentRelationship) and the
    // trusted tenant claim. A parent only ever sees children explicitly linked to
    // them with CanViewProgress = true. No client-side computation, no AI, and no
    // fabricated data — empty data returns an empty result.
    // =========================================================================

    /// <summary>Parent dashboard summary (count of linked, progress-permitted children).</summary>
    public class ParentDashboardDto
    {
        public string ParentId { get; set; } = string.Empty;
        public int LinkedChildrenCount { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// A child the parent is linked to, with relationship metadata, current class
    /// enrolment, and a server-aggregated academic status snapshot.
    /// </summary>
    public class ParentChildDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? GradeId { get; set; }
        public string? GradeName { get; set; }

        // Relationship metadata (from the parent-student link).
        public string Relationship { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public bool CanViewProgress { get; set; }
        public bool CanRequestDocuments { get; set; }
        public bool CanContactTeachers { get; set; }

        // Current active enrolment (nullable when the child is not enrolled).
        public string? ClassId { get; set; }
        public string? ClassName { get; set; }

        // Academic status snapshot (server-aggregated; same figures as the
        // relationship-authorized progress-summary endpoint).
        public int LessonsTracked { get; set; }
        public int LessonsCompleted { get; set; }
        public decimal AverageLessonCompletion { get; set; }
        public int QuizAttempts { get; set; }
        public decimal AverageQuizPercentage { get; set; }
        public int SubjectsTracked { get; set; }
    }

    /// <summary>Detailed overview of a single linked child for the child-monitoring view.</summary>
    public class ParentChildOverviewDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? GradeId { get; set; }
        public string? GradeName { get; set; }

        public string Relationship { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public bool CanViewProgress { get; set; }
        public bool CanRequestDocuments { get; set; }
        public bool CanContactTeachers { get; set; }

        public string? ClassId { get; set; }
        public string? ClassName { get; set; }
        public string? AcademicYearId { get; set; }

        /// <summary>Server-aggregated progress summary (same shape as the student progress endpoint).</summary>
        public ProgressSummaryDto Summary { get; set; } = new();
    }
}
