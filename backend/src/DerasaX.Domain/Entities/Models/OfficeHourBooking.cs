using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class OfficeHourBooking : AuditableEntity<string>
    {
        public string OfficeHourSessionId { get; set; } = null!;
        public OfficeHourSession OfficeHourSession { get; set; } = null!;
        public string StudentId { get; set; } = null!;
        public Student Student { get; set; } = null!;
        public OfficeHourBookingStatus Status { get; set; } = OfficeHourBookingStatus.Requested;
        public DateTime BookedAt { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
    }
}
