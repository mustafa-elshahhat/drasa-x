using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class Suggestion : AuditableEntity<string>
    {
        public string SubmittedByUserId { get; set; }
        public ApplicationUser SubmittedByUser { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public SuggestionStatus Status { get; set; } = SuggestionStatus.Submitted;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public string? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
    }
}
