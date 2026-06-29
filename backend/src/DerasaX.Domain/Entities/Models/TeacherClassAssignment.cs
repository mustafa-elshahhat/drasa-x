using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;
using System;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// Authorizes a teacher for a class/section (optionally for a specific subject).
    /// Tenant-owned. Same-tenant integrity with the class is enforced by a composite FK;
    /// with the teacher (a user) by a database trigger. Used to prove which classes (and
    /// therefore students) a teacher may access.
    /// </summary>
    public class TeacherClassAssignment : AuditableEntity<string>
    {
        public string TeacherId { get; set; } = string.Empty;
        public Teacher? Teacher { get; set; }

        public string SchoolClassId { get; set; } = string.Empty;
        public SchoolClass? SchoolClass { get; set; }

        public string? SubjectId { get; set; }

        public TeacherClassRole Role { get; set; } = TeacherClassRole.SubjectTeacher;
        public bool IsActive { get; set; } = true;
        public DateTime ActiveFrom { get; set; }
        public DateTime? ActiveTo { get; set; }
    }
}
