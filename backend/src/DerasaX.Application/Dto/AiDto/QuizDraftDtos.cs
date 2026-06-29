using System.Collections.Generic;

namespace DerasaX.Application.Dto.AiDto
{
    // ---------------------------------------------------------------------
    // Caller-facing (TeacherOrSchoolAdmin) request for an AI quiz draft.
    // Tenant is derived from the access token, never this body.
    // ---------------------------------------------------------------------
    public class GenerateQuizDraftDto
    {
        public string SubjectId { get; set; } = string.Empty;
        public string? LessonId { get; set; }
        public int? Grade { get; set; }
        public string? Unit { get; set; }
        public string? Topic { get; set; }
        public int NumQuestions { get; set; } = 5;
        public string Difficulty { get; set; } = "core";        // remedial | core | advanced
        public List<string>? QuestionTypes { get; set; }         // mcq | true_false
        public string Language { get; set; } = "en";
    }

    public class QuizDraftResultDto
    {
        public string QuizId { get; set; } = string.Empty;
        public string QuizGenerationId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;       // Draft
        public string Origin { get; set; } = string.Empty;       // AiGenerated
        public string Title { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public bool Grounded { get; set; }
        public int CitationCount { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string PromptVersion { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
    }

    // ---------------------------------------------------------------------
    // Internal wire contract (snake_case) mirrored from school-ai-rag.
    // ---------------------------------------------------------------------
    public class AiQuizDraftRequest
    {
        public string CorrelationId { get; set; } = string.Empty;
        public int NumQuestions { get; set; } = 5;
        public string Language { get; set; } = "en";
        public int? Grade { get; set; }
        public string? Subject { get; set; }
        public string? UnitId { get; set; }
        public string? LessonId { get; set; }
        public string? Topic { get; set; }
        public string Difficulty { get; set; } = "core";
        public List<string> QuestionTypes { get; set; } = new() { "mcq" };
        public string? PromptVersion { get; set; }
        public int TopK { get; set; } = 6;
    }

    public class AiQuizDraftResponse
    {
        public AiQuizDraft Draft { get; set; } = new();
        public bool Grounded { get; set; }
        public List<AiTutorCitation> Citations { get; set; } = new();
        public string Provider { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public string PromptVersion { get; set; } = string.Empty;
        public int RetrievalCount { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public string GeneratedAt { get; set; } = string.Empty;
    }

    public class AiQuizDraft
    {
        public string Title { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public int? Grade { get; set; }
        public string? Subject { get; set; }
        public string? Unit { get; set; }
        public string? Lesson { get; set; }
        public string Difficulty { get; set; } = "core";
        public int QuestionCount { get; set; }
        public List<AiQuizDraftQuestion> Questions { get; set; } = new();
    }

    public class AiQuizDraftQuestion
    {
        public string QuestionType { get; set; } = "mcq";
        public string QuestionText { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public int CorrectIndex { get; set; }
        public string Explanation { get; set; } = string.Empty;
        public int Points { get; set; } = 1;
        public List<string> SourceReferences { get; set; } = new();
    }
}
