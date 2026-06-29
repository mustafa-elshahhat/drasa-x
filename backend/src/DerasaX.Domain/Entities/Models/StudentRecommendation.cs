using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class StudentRecommendation : AuditableEntity<string>
    {
        public string StudentId { get; set; } = null!;
        public Student Student { get; set; } = null!;
        public string? StudentInsightId { get; set; }
        public StudentInsight? StudentInsight { get; set; }
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public RecommendationStatus Status { get; set; } = RecommendationStatus.Open;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DueAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
