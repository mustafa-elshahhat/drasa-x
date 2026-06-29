using System;
using DerasaX.Application.Common;

namespace DerasaX.Application.Dto.AcademicDto
{
    /// <summary>Response contract for an academic year (never exposes the entity directly).</summary>
    public class AcademicYearDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class AddAcademicYearDto
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class UpdateAcademicYearDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrent { get; set; }
    }

    /// <summary>Query/filter parameters for the academic-year list endpoint.</summary>
    public class AcademicYearParameters : PaginationParameters
    {
        /// <summary>When true, return only the current academic year(s).</summary>
        public bool? IsCurrent { get; set; }
    }
}
