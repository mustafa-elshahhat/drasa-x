using DerasaX.Domain.Entities.Base;

namespace DerasaX.Domain.Entities.Models
{
    public class StudentBadge : AuditableEntity<string>
    {
        public string StudentId { get; set; }
        public Student Student { get; set; }
        public string BadgeId { get; set; }
        public Badge Badge { get; set; }
        public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
        public string? AwardedReason { get; set; }
    }
}
