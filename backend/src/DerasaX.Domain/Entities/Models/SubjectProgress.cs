using DerasaX.Domain.Entities.Base;

namespace DerasaX.Domain.Entities.Models
{
    public class SubjectProgress : AuditableEntity<string>
    {
        public string StudentId { get; set; } = null!;
        public Student Student { get; set; } = null!;
        public string SubjectId { get; set; } = null!;
        public Subject Subject { get; set; } = null!;
        public decimal CompletionPercentage { get; set; }
        public decimal AverageScore { get; set; }
        public int LessonsCompleted { get; set; }
        public int TotalLessons { get; set; }
        public DateTime? LastActivityAt { get; set; }
    }
}
