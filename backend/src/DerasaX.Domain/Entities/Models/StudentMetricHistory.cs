using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class StudentMetricHistory : AuditableEntity<string>
    {
        public string StudentId { get; set; }
        public Student Student { get; set; }
        public ProgressMetricType MetricType { get; set; }
        public decimal Value { get; set; }
        public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
        public string? SourceEntityType { get; set; }
        public string? SourceEntityId { get; set; }
        public string? Notes { get; set; }
    }
}
