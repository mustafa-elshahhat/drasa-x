using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// Defines who an <see cref="Assignment"/> is given to: a whole class, an
    /// individual student, or a grade. Tenant-owned. Same-tenant integrity is enforced
    /// by composite FK to the assignment and class, and by a trigger for student.
    /// </summary>
    public class AssignmentTarget : AuditableEntity<string>
    {
        public string AssignmentId { get; set; } = string.Empty;
        public Assignment? Assignment { get; set; }

        public AssignmentTargetType TargetType { get; set; }

        public string? SchoolClassId { get; set; }
        public SchoolClass? SchoolClass { get; set; }

        public string? StudentId { get; set; }
        public Student? Student { get; set; }

        public string? GradeId { get; set; }
    }
}
