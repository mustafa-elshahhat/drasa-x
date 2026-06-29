using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.AiDto;

namespace DerasaX.Application.Services.Abstractions.Ai
{
    /// <summary>
    /// Conversation / pain-point analysis orchestration (Phase 6 §13). Only a
    /// teacher or school admin may generate analysis; access is enforced by
    /// <see cref="IStudentAccessAuthorizer"/>. Outputs are persisted as
    /// <c>PainPoint</c> records with mandatory human-review state and model/prompt
    /// versions; parents receive an approved-only safe projection.
    /// </summary>
    public interface IAnalysisService
    {
        Task<AnalysisResultDto> GenerateAsync(GenerateAnalysisDto request, CancellationToken ct = default);
        Task ReviewAsync(string painPointId, ReviewPainPointDto decision, CancellationToken ct = default);
        Task<object> GetHistoryForCallerAsync(string studentId, CancellationToken ct = default);
    }
}
