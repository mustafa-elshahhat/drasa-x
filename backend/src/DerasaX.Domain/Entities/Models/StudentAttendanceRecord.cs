using System;
using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class StudentAttendanceRecord : AuditableEntity<string>
    {
        public string StudentId { get; set; } = string.Empty;
        public Student? Student { get; set; }

        public string? SchoolClassId { get; set; }
        public SchoolClass? SchoolClass { get; set; }

        public string SessionKey { get; set; } = "day";
        public DateTime AttendanceDate { get; set; }
        public AttendanceStatus Status { get; set; }
        public DateTime RecordedAt { get; set; }
        public AttendanceSource Source { get; set; } = AttendanceSource.Manual;
        public string? Notes { get; set; }
    }
}
