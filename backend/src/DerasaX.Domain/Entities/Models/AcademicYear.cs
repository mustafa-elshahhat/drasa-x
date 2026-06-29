using DerasaX.Domain.Entities.Base;
using System;
using System.Collections.Generic;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>A school year boundary (e.g. 2025/2026). Tenant-owned.</summary>
    public class AcademicYear : AuditableEntity<string>
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrent { get; set; }

        public ICollection<Term> Terms { get; set; } = new HashSet<Term>();
        public ICollection<SchoolClass> Classes { get; set; } = new HashSet<SchoolClass>();
    }
}
