using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.AiDto;

namespace DerasaX.Application.Services.Abstractions.Ai
{
    /// <summary>
    /// AI quiz-draft orchestration (Phase 6 §11). Validates teacher/curriculum
    /// access, calls school-ai-rag for a grounded draft, revalidates it, and
    /// persists a Draft/Origin=AiGenerated quiz (with questions, options, correct
    /// answers, QuizGeneration provenance, and AiUsage) for mandatory teacher
    /// review. Never auto-publishes or auto-assigns.
    /// </summary>
    public interface IQuizDraftService
    {
        Task<QuizDraftResultDto> GenerateDraftAsync(GenerateQuizDraftDto request, CancellationToken ct = default);
    }
}
