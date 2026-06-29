using DerasaX.Domain.Entities.Base;

namespace DerasaX.Domain.Entities.Models
{
    public class CompetitionScore : AuditableEntity<string>
    {
        public string CompetitionEntryId { get; set; } = null!;
        public CompetitionEntry CompetitionEntry { get; set; } = null!;
        public decimal Score { get; set; }
        public int Rank { get; set; }
        public DateTime ScoredAt { get; set; } = DateTime.UtcNow;
    }
}
