using System;
using System.Collections.Generic;

namespace DerasaX.Application.Dto.AiDto
{
    // ---------------------------------------------------------------------
    // Caller-facing (TeacherOrSchoolAdmin generate). Conversation is provided
    // by the requester; tenant/student access enforced by the backend.
    // ---------------------------------------------------------------------
    public class GenerateAnalysisDto
    {
        public string StudentId { get; set; } = string.Empty;
        public List<AnalysisTurnDto> Conversation { get; set; } = new();
        public string? Subject { get; set; }
        public string? UnitId { get; set; }
        public string? LessonId { get; set; }
        public string Language { get; set; } = "en";
    }

    public class AnalysisTurnDto
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
    }

    public class ReviewPainPointDto
    {
        /// <summary>"approve" | "reject".</summary>
        public string Decision { get; set; } = string.Empty;
    }

    public class AnalysisResultDto
    {
        public string PainPointId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string? Unit { get; set; }
        public string? Lesson { get; set; }
        public string EvidenceSummary { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public decimal Confidence { get; set; }
        public string EscalationLevel { get; set; } = string.Empty;
        public string ReviewStatus { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public string PromptVersion { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
    }

    /// <summary>Full internal projection (teacher / school admin).</summary>
    public class PainPointReviewItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? EvidenceSummary { get; set; }
        public string? Recommendation { get; set; }
        public decimal Confidence { get; set; }
        public string EscalationLevel { get; set; } = string.Empty;
        public string ReviewStatus { get; set; } = string.Empty;
        public string? ModelVersion { get; set; }
        public string? PromptVersion { get; set; }
        public DateTime DetectedAt { get; set; }
    }

    /// <summary>Parent-safe projection: APPROVED items only, no internal evidence/escalation.</summary>
    public class ParentSafePainPointDto
    {
        public string Category { get; set; } = string.Empty;
        public string? Recommendation { get; set; }
        public string ReviewStatus { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
    }

    // ---------------------------------------------------------------------
    // Internal wire contract (snake_case) mirrored from school-ai-rag.
    // ---------------------------------------------------------------------
    public class AiAnalysisRequest
    {
        public string CorrelationId { get; set; } = string.Empty;
        public string StudentRef { get; set; } = string.Empty;
        public List<AiAnalysisTurn> Conversation { get; set; } = new();
        public string Language { get; set; } = "en";
        public string? Subject { get; set; }
        public string? UnitId { get; set; }
        public string? LessonId { get; set; }
    }

    public class AiAnalysisTurn
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
    }

    public class AiAnalysisResponse
    {
        public string StudentRef { get; set; } = string.Empty;
        public string PainPointCategory { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string? Unit { get; set; }
        public string? Lesson { get; set; }
        public string EvidenceSummary { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string EscalationLevel { get; set; } = string.Empty;
        public bool HumanReviewRequired { get; set; }
        public List<string> Signals { get; set; } = new();
        public string Model { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public string PromptVersion { get; set; } = string.Empty;
        public string GeneratedAt { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
    }
}
