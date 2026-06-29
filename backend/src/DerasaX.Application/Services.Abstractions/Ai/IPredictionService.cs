using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.AiDto;

namespace DerasaX.Application.Services.Abstractions.Ai
{
    /// <summary>
    /// Backend-mediated performance prediction (Phase 6 §12). Enforces student-access
    /// rules, derives the approved feature schema from authoritative tenant-scoped
    /// records, calls school-ai-rag for inference only, revalidates the result, and
    /// appends an immutable historical <c>PredictionRecord</c> with model/feature-schema
    /// versions. The browser never supplies academic metrics.
    /// </summary>
    public interface IPredictionService
    {
        Task<PredictionResultDto> GenerateAsync(GeneratePredictionDto request, CancellationToken ct = default);
        Task<IReadOnlyList<PredictionHistoryItemDto>> GetHistoryAsync(string studentId, CancellationToken ct = default);
        Task UpsertLearningProfileAsync(string studentId, SetLearningProfileDto profile, CancellationToken ct = default);
    }
}
