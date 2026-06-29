using DerasaX.Domain.Entities.Base;
using System;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// Authorizes a teacher to teach a subject. Tenant-owned. Same-tenant integrity
    /// with the subject is enforced by a composite FK; with the teacher (a user) by a
    /// database trigger. Used to prove which subjects a teacher may access.
    /// </summary>
    public class TeacherSubjectAssignment : AuditableEntity<string>
    {
        public string TeacherId { get; set; } = string.Empty;
        public Teacher? Teacher { get; set; }

        public string SubjectId { get; set; } = string.Empty;
        public Subject? Subject { get; set; }

        public string? AcademicYearId { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime ActiveFrom { get; set; }
        public DateTime? ActiveTo { get; set; }
    }
}
