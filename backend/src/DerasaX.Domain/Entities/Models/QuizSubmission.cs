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
    public class QuizSubmission :BaseEntity<string>
    {
        public int AchievedScore { get; set; }        
        public int TotalScore { get; set; }      
        public double Percentage => TotalScore > 0 ? (double)AchievedScore / TotalScore * 100 : 0;
        public string? TeacherFeedback { get; set; }
        public SubmissionStatus submissionStatus { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Attempt sequence number (1-based) — multiple attempts give attempt history.</summary>
        public int AttemptNumber { get; set; } = 1;
        /// <summary>True for the student's most recent attempt at the quiz.</summary>
        public bool IsLatestAttempt { get; set; } = true;
        public DateTime? StartedAt { get; set; }

        // Grading metadata (manual vs automatic).
        public GradingMethod GradingMethod { get; set; } = GradingMethod.Automatic;
        public string? GradedByTeacherId { get; set; }
        public DateTime? GradedAt { get; set; }

        /// <summary>Optional link to the assignment this submission fulfills.</summary>
        public string? AssignmentId { get; set; }

        [ForeignKey("Student")]
        public string StudentId { get; set; } = null!;
        public Student Student { get; set; } = null!;
        [ForeignKey("Quiz")]
        public string QuizId { get; set; } = null!;
        public Quiz Quiz { get; set; } = null!;
        public ICollection<SubmissionAnswer> SubmissionAnswers { get; set; } = new HashSet<SubmissionAnswer>();

    }
}
