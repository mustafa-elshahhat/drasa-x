using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class Suggestion : AuditableEntity<string>
    {
        public string SubmittedByUserId { get; set; } = null!;
        public ApplicationUser SubmittedByUser { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public SuggestionStatus Status { get; set; } = SuggestionStatus.Submitted;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public string? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
    }
}
