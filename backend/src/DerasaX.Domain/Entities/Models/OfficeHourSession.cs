using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class OfficeHourSession : AuditableEntity<string>
    {
        public string TeacherId { get; set; } = null!;
        public Teacher Teacher { get; set; } = null!;
        public string Title { get; set; } = null!;
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public int Capacity { get; set; }
        public OfficeHourStatus Status { get; set; } = OfficeHourStatus.Scheduled;
        public ICollection<OfficeHourBooking> Bookings { get; set; } = new HashSet<OfficeHourBooking>();
    }
}
