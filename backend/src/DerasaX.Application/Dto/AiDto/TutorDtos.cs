using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DerasaX.Application.Dto.AiDto
{
    // ---------------------------------------------------------------------
    // Frontend-facing tutor contract (POST /api/chat). Field names match the
    // existing school-ai-frontend payload (snake_case) so the Phase 6 backend
    // proxy preserves the established contract. Tenant/user are NEVER read from
    // this body — they come from the signed access token.
    // ---------------------------------------------------------------------
    public class TutorChatRequestDto
    {
        [JsonPropertyName("conversation_id")] public string? ConversationId { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
        [JsonPropertyName("student_name")] public string? StudentName { get; set; }
        [JsonPropertyName("grade")] public int? Grade { get; set; }
        [JsonPropertyName("subject")] public string? Subject { get; set; }
        [JsonPropertyName("language")] public string? Language { get; set; }
        [JsonPropertyName("history")] public List<TutorTurnDto>? History { get; set; }
    }

    public class TutorTurnDto
    {
        [JsonPropertyName("role")] public string Role { get; set; } = "user";
        [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
    }

    public class TutorChatResponseDto
    {
        public string Answer { get; set; } = string.Empty;
        public bool Grounded { get; set; }
        public string? NoAnswerReason { get; set; }
        public List<TutorCitationDto> Citations { get; set; } = new();
        public string Provider { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public string PromptVersion { get; set; } = string.Empty;
        public int RetrievalCount { get; set; }
        public int CitationCount { get; set; }
        public int LatencyMs { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
    }

    public class TutorCitationDto
    {
        public string SourceDocumentId { get; set; } = string.Empty;
        public string ChunkId { get; set; } = string.Empty;
        public double Score { get; set; }
        public string? Title { get; set; }
        public int? Grade { get; set; }
        public string? Subject { get; set; }
        public string? Unit { get; set; }
        public string? Lesson { get; set; }
        public string? Snippet { get; set; }
    }

    // ---------------------------------------------------------------------
    // Internal wire contract mirrored from school-ai-rag /internal/v1/tutor.
    // Serialized/deserialized with a snake_case naming policy by AiRagClient,
    // so PascalCase here maps to the Python schema (correlation_id, top_k, ...).
    // ---------------------------------------------------------------------
    public class AiTutorRequest
    {
        public string CorrelationId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
        public int? Grade { get; set; }
        public string? Subject { get; set; }
        public string? UnitId { get; set; }
        public string? LessonId { get; set; }
        public List<AiTutorTurn> History { get; set; } = new();
        public string? PromptVersion { get; set; }
        public int TopK { get; set; } = 4;
    }

    public class AiTutorTurn
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
    }

    public class AiTutorResponse
    {
        public string Answer { get; set; } = string.Empty;
        public bool Grounded { get; set; }
        public string? NoAnswerReason { get; set; }
        public List<AiTutorCitation> Citations { get; set; } = new();
        public string Provider { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public string PromptVersion { get; set; } = string.Empty;
        public int RetrievalCount { get; set; }
        public int CitationCount { get; set; }
        public int LatencyMs { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
    }

    public class AiTutorCitation
    {
        public string SourceDocumentId { get; set; } = string.Empty;
        public string ChunkId { get; set; } = string.Empty;
        public double Score { get; set; }
        public string? Title { get; set; }
        public int? Grade { get; set; }
        public string? Subject { get; set; }
        public string? Unit { get; set; }
        public string? Lesson { get; set; }
        public string? Snippet { get; set; }
    }
}
