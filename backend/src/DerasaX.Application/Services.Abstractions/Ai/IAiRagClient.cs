using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.AiDto;

namespace DerasaX.Application.Services.Abstractions.Ai
{
    /// <summary>
    /// Typed client for the internal school-ai-rag contract. Every call mints a
    /// fresh short-lived signed service token carrying the trusted tenant claim;
    /// the tenant is never sent in the body. Failures surface as
    /// <see cref="DerasaX.Domain.Exceptions.AiServiceException"/> with a safe message.
    /// </summary>
    public interface IAiRagClient
    {
        /// <param name="tenantId">Trusted tenant id (from the access-token claim).</param>
        /// <param name="actorUserId">Opaque actor id for audit (never PII).</param>
        Task<AiTutorResponse> TutorAsync(AiTutorRequest request, string tenantId, string? actorUserId, CancellationToken ct = default);

        /// <summary>Ingest/re-index one tenant-scoped curriculum document (scope ai:ingest).</summary>
        Task<AiIngestResponse> IngestDocumentAsync(AiIngestRequest request, string tenantId, string? actorUserId, CancellationToken ct = default);

        /// <summary>Delete all chunks of a tenant-scoped document (scope ai:ingest).</summary>
        Task<AiDeleteResponse> DeleteDocumentAsync(string documentId, string correlationId, string tenantId, string? actorUserId, CancellationToken ct = default);

        /// <summary>Generate a grounded, draft-only quiz (scope ai:quiz). No AI-side persistence.</summary>
        Task<AiQuizDraftResponse> QuizDraftAsync(AiQuizDraftRequest request, string tenantId, string? actorUserId, CancellationToken ct = default);

        /// <summary>Run performance inference on backend-derived features (scope ai:prediction). Inference only.</summary>
        Task<AiPredictionResponse> PredictAsync(AiPredictionRequest request, string tenantId, string? actorUserId, CancellationToken ct = default);

        /// <summary>Analyze a conversation for learning pain points (scope ai:analyze). Non-diagnostic; no AI-side persistence.</summary>
        Task<AiAnalysisResponse> AnalyzeAsync(AiAnalysisRequest request, string tenantId, string? actorUserId, CancellationToken ct = default);

        /// <summary>
        /// Analyze ONE ephemeral classroom frame (Phase 15, scope ai:vision). The AI
        /// returns normalized per-face recognition candidates / emotion / engagement —
        /// never DerasaX identities and never the image. The backend owns persistence
        /// and identity mapping.
        /// </summary>
        Task<AiVisionAnalyzeResponse> AnalyzeVisionFrameAsync(AiVisionAnalyzeRequest request, string tenantId, string? actorUserId, CancellationToken ct = default);

        /// <summary>Clear the AI service's ephemeral per-track engagement buffers for a finished session (scope ai:vision).</summary>
        Task<AiVisionEndSessionResponse> EndVisionSessionAsync(AiVisionEndSessionRequest request, string tenantId, string? actorUserId, CancellationToken ct = default);
    }
}
