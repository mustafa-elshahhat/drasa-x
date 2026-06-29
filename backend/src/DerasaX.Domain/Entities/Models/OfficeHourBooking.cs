using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class OfficeHourBooking : AuditableEntity<string>
    {
        public string OfficeHourSessionId { get; set; }
        public OfficeHourSession OfficeHourSession { get; set; }
        public string StudentId { get; set; }
        public Student Student { get; set; }
        public OfficeHourBookingStatus Status { get; set; } = OfficeHourBookingStatus.Requested;
        public DateTime BookedAt { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
    }
}
