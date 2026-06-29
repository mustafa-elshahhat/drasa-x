using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class PainPoint : AuditableEntity<string>
    {
        public string StudentId { get; set; }
        public Student Student { get; set; }
        public string? StudentInsightId { get; set; }
        public StudentInsight? StudentInsight { get; set; }
        public PainPointCategory Category { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public decimal ConfidenceScore { get; set; }
        public bool IsResolved { get; set; }
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }

        // ---- Phase 6 §13: AI analysis provenance + human review (nullable; back-compat) ----
        /// <summary>A constructive next learning step (non-diagnostic).</summary>
        public string? Recommendation { get; set; }
        /// <summary>Pedagogical follow-up level — a request for human attention, never automatic action.</summary>
        public EscalationLevel Escalation { get; set; } = EscalationLevel.None;
        /// <summary>Mandatory human-review state for an AI-generated pain point.</summary>
        public HumanReviewStatus ReviewStatus { get; set; } = HumanReviewStatus.Pending;
        public string? ReviewedByTeacherId { get; set; }
        public DateTime? ReviewedAt { get; set; }

        // AI provenance.
        public string? AiProvider { get; set; }
        public string? ModelVersion { get; set; }
        public string? PromptVersion { get; set; }
        public string? CorrelationId { get; set; }
    }
}
