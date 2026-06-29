using DerasaX.Domain.Entities.Base;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// Phase 14 (closure) — a student's durable submitted work for a competition. Bridges the
    /// entry→score model with the actual content a student submits to be judged: a student must
    /// have entered, may submit/resubmit while the competition is open, and staff read submissions
    /// to score them. One submission per (tenant, competition, student); content is updated in place.
    /// </summary>
    public class CompetitionSubmission : AuditableEntity<string>
    {
        public string CompetitionId { get; set; }
        public Competition Competition { get; set; }
        public string StudentId { get; set; }
        public Student Student { get; set; }
        public string Content { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
