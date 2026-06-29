using DerasaX.Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DerasaX.Domain.Entities.Models
{
    public class StudentLessonProgress : AuditableEntity<string>
    {
        public DateTime? StartedAt { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletedAt { get; set; }
        public decimal CompletionPercentage { get; set; }
        public int TimeSpentSeconds { get; set; }
        public int WatchedMaterialsCount { get; set; }
        [ForeignKey("Student")]
        public string StudentId { get; set; } = null!;
        public Student Student { get; set; } = null!;
        [ForeignKey("Lesson")]
        public string LessonId { get; set; } = null!;
        public Lesson Lesson { get; set; } = null!;

    }
}
