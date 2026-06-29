using System;
using DerasaX.Application.Common;

namespace DerasaX.Application.Dto.AcademicDto
{
    public class SchoolClassDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int? Capacity { get; set; }
        public string GradeId { get; set; } = string.Empty;
        public string AcademicYearId { get; set; } = string.Empty;
        public int EnrolledCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class AddSchoolClassDto
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int? Capacity { get; set; }
        public string GradeId { get; set; } = string.Empty;
        public string AcademicYearId { get; set; } = string.Empty;
    }

    public class UpdateSchoolClassDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int? Capacity { get; set; }
    }

    public class SchoolClassParameters : PaginationParameters
    {
        public string? GradeId { get; set; }
        public string? AcademicYearId { get; set; }
    }
}
