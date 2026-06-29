using DerasaX.Domain.Entities.Base;

namespace DerasaX.Domain.Entities.Models
{
    public class StudentStreak : AuditableEntity<string>
    {
        public string StudentId { get; set; }
        public Student Student { get; set; }
        public int CurrentCount { get; set; }
        public int LongestCount { get; set; }
        public DateTime LastActivityDate { get; set; }
    }
}
