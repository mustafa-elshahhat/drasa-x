using System;
using System.Collections.Generic;

namespace DerasaX.Application.Dto.TeacherDto
{
    // =========================================================================
    // Phase 9 — Teacher Portal read models. Every figure is aggregated
    // server-side from authoritative records and scoped to the teacher's active
    // assignments (TeacherClassAssignment / TeacherSubjectAssignment) and tenant.
    // SchoolAdmin sees the whole tenant. No client-side computation, no AI.
    // =========================================================================

    public class TeacherClassDto
    {
        public string ClassId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? GradeId { get; set; }
        public int StudentCount { get; set; }
    }

    public class TeacherSubjectDto
    {
        public string SubjectId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? GradeId { get; set; }
        public int QuizCount { get; set; }
    }

    public class TeacherStudentDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string ClassId { get; set; } = string.Empty;
    }

    public class TeacherDashboardDto
    {
        public string TeacherId { get; set; } = string.Empty;
        public int AssignedClassCount { get; set; }
        public int AssignedSubjectCount { get; set; }
        public int StudentCount { get; set; }
        public int DraftQuizCount { get; set; }
        public int PublishedQuizCount { get; set; }
        public int PendingGradingCount { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
