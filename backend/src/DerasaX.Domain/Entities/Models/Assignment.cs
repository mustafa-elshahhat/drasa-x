using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;
using System;
using System.Collections.Generic;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// A homework/assignment given to students. May wrap a <see cref="Quiz"/> or be a
    /// standalone task. Tenant-owned. Targeting (which classes/students/grades receive
    /// it) is modeled via <see cref="AssignmentTarget"/>.
    /// </summary>
    public class Assignment : AuditableEntity<string>
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public AssignmentType Type { get; set; } = AssignmentType.Homework;
        public AssignmentStatus Status { get; set; } = AssignmentStatus.Draft;

        public DateTime? AvailableFrom { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal? MaxScore { get; set; }

        /// <summary>Teacher who created/assigned the work (audit reference).</summary>
        public string? AssignedByTeacherId { get; set; }

        // Optional anchors (same-tenant composite FK to Quiz/Subject).
        public string? QuizId { get; set; }
        public Quiz? Quiz { get; set; }
        public string? SubjectId { get; set; }
        public Subject? Subject { get; set; }
        public string? LessonId { get; set; }

        public ICollection<AssignmentTarget> Targets { get; set; } = new HashSet<AssignmentTarget>();
    }
}
