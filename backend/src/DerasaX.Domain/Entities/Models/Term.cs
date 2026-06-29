using DerasaX.Domain.Entities.Base;
using System;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>A term/semester within an <see cref="AcademicYear"/>. Tenant-owned.</summary>
    public class Term : AuditableEntity<string>
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Order { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string AcademicYearId { get; set; } = string.Empty;
        public AcademicYear? AcademicYear { get; set; }
    }
}
