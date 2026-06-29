using DerasaX.Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DerasaX.Domain.Entities.Models
{
    public class QuestionOption :BaseEntity<string>
    {
        public string Text { get; set; } = null!;
        public bool IsCorrect { get; set; } = false;
        [ForeignKey("Question")]
        public string QuestionId { get; set; } = null!;
        public Question Question { get; set; } = null!;
        public ICollection<SubmissionAnswer> SubmissionAnswers { get; set; } = new HashSet<SubmissionAnswer>();
    }
}
