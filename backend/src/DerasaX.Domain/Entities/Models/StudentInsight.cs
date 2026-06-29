using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DerasaX.Domain.Entities.Models
{
    public class StudentInsight : AuditableEntity<string>
    {
        public PerformanceLevel Performance { get; set; }
        public decimal ConfidenceScore { get; set; }
        public string? Summary { get; set; }
        public string? EvidenceJson { get; set; }
        public string? RecommendationText { get; set; }
        public InsightPeriod Period { get; set; } = InsightPeriod.Weekly;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public DateTime PeriodStart { get; set; }       
        public DateTime PeriodEnd { get; set; }
        [ForeignKey("Student")]
        public string StudentId { get; set; }
        public Student Student { get; set; }
        public ICollection<PainPoint> PainPoints { get; set; } = new HashSet<PainPoint>();
        public ICollection<StudentRecommendation> Recommendations { get; set; } = new HashSet<StudentRecommendation>();
    }
}
