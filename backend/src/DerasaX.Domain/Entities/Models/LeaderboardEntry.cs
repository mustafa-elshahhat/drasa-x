using DerasaX.Domain.Entities.Base;

namespace DerasaX.Domain.Entities.Models
{
    public class LeaderboardEntry : AuditableEntity<string>
    {
        public string CompetitionId { get; set; } = null!;
        public Competition Competition { get; set; } = null!;
        public string StudentId { get; set; } = null!;
        public Student Student { get; set; } = null!;
        public decimal Score { get; set; }
        public int Rank { get; set; }
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }
}
