using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.AiDto;

namespace DerasaX.Application.Services.Abstractions.Ai
{
    /// <summary>
    /// Backend-mediated AI tutor orchestration (Phase 6 §9): validates access,
    /// derives trusted tenant/curriculum context, calls school-ai-rag, records AI
    /// usage, and returns a normalized grounded response with citations.
    /// </summary>
    public interface ITutorService
    {
        Task<TutorChatResponseDto> AskAsync(TutorChatRequestDto request, CancellationToken ct = default);
    }
}
