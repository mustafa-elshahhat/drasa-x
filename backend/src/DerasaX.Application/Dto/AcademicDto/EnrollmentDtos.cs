using System;
using DerasaX.Application.Common;
using DerasaX.Domain.Enums;

namespace DerasaX.Application.Dto.AcademicDto
{
    public class EnrollmentDto
    {
        public string Id { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string SchoolClassId { get; set; } = string.Empty;
        public string AcademicYearId { get; set; } = string.Empty;
        public EnrollmentStatus Status { get; set; }
        public DateTime EnrolledAt { get; set; }
        public DateTime? WithdrawnAt { get; set; }
        public string? WithdrawalReason { get; set; }
    }

    public class AddEnrollmentDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string SchoolClassId { get; set; } = string.Empty;
    }

    public class WithdrawEnrollmentDto
    {
        public string Id { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    public class EnrollmentParameters : PaginationParameters
    {
        public string? SchoolClassId { get; set; }
        public string? StudentId { get; set; }
        public EnrollmentStatus? Status { get; set; }
    }
}
