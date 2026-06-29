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
    public class Quiz:BaseEntity<string>
    {
        public string? Title { get; set; }
        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Core;
        public DateTime? DueDate { get; set; }
        public int TimeLimitMinutes { get; set; } = 30;
        public QuizType Type { get; set; } = QuizType.Lesson;

        /// <summary>Lifecycle: distinguishes an AI draft from a teacher-approved/published quiz.</summary>
        public QuizStatus Status { get; set; } = QuizStatus.Draft;
        public QuizOrigin Origin { get; set; } = QuizOrigin.Manual;

        /// <summary>Teacher who reviewed/approved an (AI or manual) quiz, and when.</summary>
        public string? ApprovedByTeacherId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ReviewedByTeacherId { get; set; }
        public DateTime? ReviewedAt { get; set; }

        /// <summary>Max attempts a student may make (null = unlimited).</summary>
        public int? MaxAttempts { get; set; }

        public string? LessonId { get; set; }
        public Lesson? Lesson { get; set; }

        public string? SubjectId { get; set; }
        public Subject? Subject { get; set; }
        public ICollection<Question> Questions { get; set; } = new HashSet<Question>();
        public ICollection<QuizSubmission> QuizSubmissions { get; set; } = new HashSet<QuizSubmission>();
    }
}
