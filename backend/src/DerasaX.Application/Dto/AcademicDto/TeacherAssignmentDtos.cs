using System;
using DerasaX.Application.Common;

namespace DerasaX.Application.Dto.AcademicDto
{
    public class TeacherSubjectAssignmentDto
    {
        public string Id { get; set; } = string.Empty;
        public string TeacherId { get; set; } = string.Empty;
        public string SubjectId { get; set; } = string.Empty;
        public string? AcademicYearId { get; set; }
        public bool IsActive { get; set; }
        public DateTime ActiveFrom { get; set; }
        public DateTime? ActiveTo { get; set; }
    }

    public class AddTeacherSubjectAssignmentDto
    {
        public string TeacherId { get; set; } = string.Empty;
        public string SubjectId { get; set; } = string.Empty;
        public string? AcademicYearId { get; set; }
    }

    public class TeacherSubjectAssignmentParameters : PaginationParameters
    {
        public string? TeacherId { get; set; }
        public string? SubjectId { get; set; }
        public bool? IsActive { get; set; }
    }
}
