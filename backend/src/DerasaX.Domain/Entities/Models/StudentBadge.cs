using DerasaX.Domain.Entities.Base;

namespace DerasaX.Domain.Entities.Models
{
    public class StudentBadge : AuditableEntity<string>
    {
        public string StudentId { get; set; } = null!;
        public Student Student { get; set; } = null!;
        public string BadgeId { get; set; } = null!;
        public Badge Badge { get; set; } = null!;
        public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
        public string? AwardedReason { get; set; }
    }
}
