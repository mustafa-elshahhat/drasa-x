using DerasaX.Domain.Entities.Base;

namespace DerasaX.Domain.Entities.Models
{
    public class CompetitionEntry : AuditableEntity<string>
    {
        public string CompetitionId { get; set; } = null!;
        public Competition Competition { get; set; } = null!;
        public string StudentId { get; set; } = null!;
        public Student Student { get; set; } = null!;
        public DateTime EnteredAt { get; set; } = DateTime.UtcNow;
        public ICollection<CompetitionScore> Scores { get; set; } = new HashSet<CompetitionScore>();
    }
}
