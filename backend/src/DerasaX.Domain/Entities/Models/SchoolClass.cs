using DerasaX.Domain.Entities.Base;
using System.Collections.Generic;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// A class/section of a grade for a given academic year (e.g. "7-A, 2025/2026").
    /// Tenant-owned. Same-tenant integrity with its Grade and AcademicYear is enforced
    /// by composite foreign keys.
    /// </summary>
    public class SchoolClass : AuditableEntity<string>
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int? Capacity { get; set; }

        public string GradeId { get; set; } = string.Empty;
        public Grade? Grade { get; set; }

        public string AcademicYearId { get; set; } = string.Empty;
        public AcademicYear? AcademicYear { get; set; }

        public ICollection<Enrollment> Enrollments { get; set; } = new HashSet<Enrollment>();
        public ICollection<TeacherClassAssignment> TeacherAssignments { get; set; } = new HashSet<TeacherClassAssignment>();
    }
}
