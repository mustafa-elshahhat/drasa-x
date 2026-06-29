using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class Competition : AuditableEntity<string>
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public CompetitionStatus Status { get; set; } = CompetitionStatus.Draft;
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public ICollection<CompetitionEntry> Entries { get; set; } = new HashSet<CompetitionEntry>();
        public ICollection<CompetitionSubmission> Submissions { get; set; } = new HashSet<CompetitionSubmission>();
    }
}
