using DerasaX.Domain.Entities.Base;

namespace DerasaX.Domain.Entities.Models
{
    public class LeaderboardEntry : AuditableEntity<string>
    {
        public string CompetitionId { get; set; }
        public Competition Competition { get; set; }
        public string StudentId { get; set; }
        public Student Student { get; set; }
        public decimal Score { get; set; }
        public int Rank { get; set; }
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }
}
