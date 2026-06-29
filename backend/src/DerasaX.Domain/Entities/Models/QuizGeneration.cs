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
    /// <summary>
    /// Record of an AI quiz-generation run: the prompt and model metadata used to
    /// produce a draft, plus the teacher review/approval outcome. Kept distinct from
    /// the resulting <see cref="Quiz"/> so the generation provenance is auditable.
    /// </summary>
    public class QuizGeneration :BaseEntity<string>
    {
        public string PromptUsed { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // AI provenance metadata.
        public string? AiProvider { get; set; }
        public string? AiModel { get; set; }
        public string? ModelVersion { get; set; }
        public string? PromptVersion { get; set; }
        public int? TokensUsed { get; set; }
        public string? CorrelationId { get; set; }

        // Review / approval workflow.
        public QuizGenerationStatus Status { get; set; } = QuizGenerationStatus.Pending;
        public string? ReviewedByTeacherId { get; set; }
        public DateTime? ReviewedAt { get; set; }
        /// <summary>Non-sensitive error category if the generation failed.</summary>
        public string? ErrorCategory { get; set; }

        [ForeignKey("Quiz")]
        public string QuizId { get; set; }
        public Quiz Quiz { get; set; }
    }
}
