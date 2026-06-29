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
    public class Question :BaseEntity<string>
    {
        public string Text { get; set; } = null!;
        public QuestionType Type { get; set; }
        public int Order { get; set; }
        public int Points { get; set; } = 1;

        /// <summary>Canonical correct answer for non-MCQ questions (MCQ uses QuestionOption.IsCorrect).</summary>
        public string? CorrectAnswerText { get; set; }
        /// <summary>Optional explanation/rationale shown after grading.</summary>
        public string? Explanation { get; set; }
        [ForeignKey("Quiz")]
        public string QuizId { get; set; } = null!;
        public Quiz Quiz { get; set; } = null!;
        public ICollection<QuestionOption> Options { get; set; } = new HashSet<QuestionOption>();
        public ICollection<SubmissionAnswer> SubmissionAnswers { get; set; } = new HashSet<SubmissionAnswer>();
    }
}
