using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class PredictionRecord : AuditableEntity<string>
    {
        public string StudentId { get; set; } = null!;
        public Student Student { get; set; } = null!;
        public PredictionKind Kind { get; set; } = PredictionKind.Performance;
        public decimal PredictedScore { get; set; }
        public PerformanceLevel Level { get; set; }
        public decimal ConfidenceScore { get; set; }
        public string? ModelName { get; set; }
        public string? ModelVersion { get; set; }
        public string? InputSnapshotJson { get; set; }
        public DateTime PredictedAt { get; set; } = DateTime.UtcNow;
    }
}
