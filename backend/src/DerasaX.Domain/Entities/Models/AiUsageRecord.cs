using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class AiUsageRecord : AuditableEntity<string>
    {
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public AiUsageKind Kind { get; set; }
        public string Provider { get; set; } = null!;
        public string? Model { get; set; }
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
        public int? TotalTokens { get; set; }
        public decimal? Cost { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime UsedAt { get; set; } = DateTime.UtcNow;
    }
}
