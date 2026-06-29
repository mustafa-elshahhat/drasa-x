namespace DerasaX.Application.Dto.AiDto
{
    // ---------------------------------------------------------------------
    // Caller-facing (TeacherOrSchoolAdmin) curriculum ingestion through the
    // backend. Tenant is derived from the access token, never this body.
    // ---------------------------------------------------------------------
    public class IngestCurriculumDocumentDto
    {
        public string DocumentId { get; set; } = string.Empty;
        public int Version { get; set; } = 1;
        public string Content { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
        public string MaterialType { get; set; } = "other";
        public string? Title { get; set; }
        public string? FileId { get; set; }
        public string? AcademicYear { get; set; }
        public int? Grade { get; set; }
        public string? Subject { get; set; }
        public string? Unit { get; set; }
        public string? Lesson { get; set; }
    }

    public class IngestResultDto
    {
        public string DocumentId { get; set; } = string.Empty;
        public int Version { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ChunkCount { get; set; }
        public int RemovedChunks { get; set; }
        public string Checksum { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string IndexedAt { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
    }

    public class DeleteResultDto
    {
        public string DocumentId { get; set; } = string.Empty;
        public int DeletedChunks { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    // ---------------------------------------------------------------------
    // Internal wire contract (snake_case) mirrored from school-ai-rag.
    // ---------------------------------------------------------------------
    public class AiIngestRequest
    {
        public string CorrelationId { get; set; } = string.Empty;
        public string DocumentId { get; set; } = string.Empty;
        public int Version { get; set; } = 1;
        public string Content { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
        public string MaterialType { get; set; } = "other";
        public string? Title { get; set; }
        public string? FileId { get; set; }
        public string? AcademicYear { get; set; }
        public int? Grade { get; set; }
        public string? Subject { get; set; }
        public string? Unit { get; set; }
        public string? Lesson { get; set; }
    }

    public class AiIngestResponse
    {
        public string DocumentId { get; set; } = string.Empty;
        public int Version { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ChunkCount { get; set; }
        public int RemovedChunks { get; set; }
        public string Checksum { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Collection { get; set; } = string.Empty;
        public string IndexedAt { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
    }

    public class AiDeleteResponse
    {
        public string DocumentId { get; set; } = string.Empty;
        public int DeletedChunks { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
