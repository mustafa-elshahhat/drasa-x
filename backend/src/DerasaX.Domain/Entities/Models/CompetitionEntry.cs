using DerasaX.Domain.Entities.Base;

namespace DerasaX.Domain.Entities.Models
{
    public class CompetitionEntry : AuditableEntity<string>
    {
        public string CompetitionId { get; set; }
        public Competition Competition { get; set; }
        public string StudentId { get; set; }
        public Student Student { get; set; }
        public DateTime EnteredAt { get; set; } = DateTime.UtcNow;
        public ICollection<CompetitionScore> Scores { get; set; } = new HashSet<CompetitionScore>();
    }
}
