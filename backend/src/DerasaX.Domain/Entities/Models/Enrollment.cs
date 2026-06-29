using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;
using System;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// A student's enrollment into a <see cref="SchoolClass"/> for an academic year,
    /// with status history. Tenant-owned. Same-tenant integrity with the class is
    /// enforced by a composite FK; same-tenant integrity with the student (a user) is
    /// enforced by a database trigger (ApplicationUser tenant is nullable for platform
    /// admins, so it cannot participate in an alternate key).
    /// </summary>
    public class Enrollment : AuditableEntity<string>
    {
        public string StudentId { get; set; } = string.Empty;
        public Student? Student { get; set; }

        public string SchoolClassId { get; set; } = string.Empty;
        public SchoolClass? SchoolClass { get; set; }

        public string AcademicYearId { get; set; } = string.Empty;

        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
        public DateTime EnrolledAt { get; set; }
        public DateTime? WithdrawnAt { get; set; }
        public string? WithdrawalReason { get; set; }
    }
}
