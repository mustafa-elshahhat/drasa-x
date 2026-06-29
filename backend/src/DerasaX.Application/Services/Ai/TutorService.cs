using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.AiDto;
using DerasaX.Application.Dto.OperationsDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Ai;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace DerasaX.Application.Services.Ai
{
    /// <summary>
    /// AI tutor orchestration. The student is already authenticated and tenant-scoped
    /// by the controller policy; this service derives tenant/user from the trusted
    /// context (never the body), calls the internal AI contract, records usage for
    /// both success and failure, and returns a normalized response with citations.
    /// Provider failures map to a stable <see cref="AiServiceException"/>; no fake
    /// success is ever returned.
    /// </summary>
    public class TutorService : ITutorService
    {
        private const int MaxHistoryTurns = 20;
        private const int MaxContentChars = 4000;
        private const string Provider = "groq";

        private readonly IAiRagClient _ai;
        private readonly ITenantContext _tenant;
        private readonly IAiUsageService _usage;
        private readonly ILogger<TutorService> _logger;

        public TutorService(IAiRagClient ai, ITenantContext tenant, IAiUsageService usage, ILogger<TutorService> logger)
        {
            _ai = ai;
            _tenant = tenant;
            _usage = usage;
            _logger = logger;
        }

        public async Task<TutorChatResponseDto> AskAsync(TutorChatRequestDto request, CancellationToken ct = default)
        {
            var tenantId = _tenant.TenantId
                ?? throw new UnauthorizedException("Tenant context is required for the AI tutor.");
            var userId = _tenant.UserId;

            if (string.IsNullOrWhiteSpace(request.Message))
                throw new BadRequestException("Message is required.");
            if (request.Message.Length > MaxContentChars)
                throw new BadRequestException("Message is too long.");

            var correlationId = Guid.NewGuid().ToString("N");
            var aiRequest = new AiTutorRequest
            {
                CorrelationId = correlationId,
                Message = request.Message,
                Language = NormalizeLanguage(request.Language),
                Grade = request.Grade,
                Subject = string.IsNullOrWhiteSpace(request.Subject) ? null : request.Subject,
                History = MapHistory(request.History),
            };

            var sw = Stopwatch.StartNew();
            AiTutorResponse aiResponse;
            try
            {
                aiResponse = await _ai.TutorAsync(aiRequest, tenantId, userId, ct);
            }
            catch (AiServiceException ex)
            {
                sw.Stop();
                await TryRecordUsageAsync(
                    model: null, failed: true, latencyMs: (int)sw.ElapsedMilliseconds,
                    correlationId: correlationId, category: ex.Category, ct: ct);
                throw;
            }

            sw.Stop();
            await TryRecordUsageAsync(
                model: aiResponse.Model, failed: false, latencyMs: aiResponse.LatencyMs > 0 ? aiResponse.LatencyMs : (int)sw.ElapsedMilliseconds,
                correlationId: aiResponse.CorrelationId, category: aiResponse.Grounded ? "grounded" : "no_answer", ct: ct);

            return Map(aiResponse);
        }

        private async Task TryRecordUsageAsync(string? model, bool failed, int latencyMs, string correlationId, string category, CancellationToken ct)
        {
            try
            {
                await _usage.RecordInternalAsync(new RecordAiUsageDto
                {
                    Kind = AiUsageKind.Chat,
                    Provider = Provider,
                    Model = model,
                    Failed = failed,
                    LatencyMs = latencyMs,
                    CorrelationId = correlationId,
                }, ct);
            }
            catch (Exception ex)
            {
                // Usage recording must never mask the primary result/error.
                _logger.LogWarning(ex, "AI usage recording failed. correlationId={CorrelationId} category={Category}", correlationId, category);
            }
        }

        private static string NormalizeLanguage(string? lang) =>
            string.Equals(lang, "ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "en";

        private static List<AiTutorTurn> MapHistory(List<TutorTurnDto>? history)
        {
            if (history is null || history.Count == 0) return new List<AiTutorTurn>();
            return history
                .Where(t => t is not null && !string.IsNullOrWhiteSpace(t.Content))
                .TakeLast(MaxHistoryTurns)
                .Select(t => new AiTutorTurn
                {
                    Role = string.Equals(t.Role, "assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user",
                    Content = t.Content.Length > MaxContentChars ? t.Content[..MaxContentChars] : t.Content,
                })
                .ToList();
        }

        private static TutorChatResponseDto Map(AiTutorResponse r) => new()
        {
            Answer = r.Answer,
            Grounded = r.Grounded,
            NoAnswerReason = r.NoAnswerReason,
            Provider = r.Provider,
            Model = r.Model,
            ModelVersion = r.ModelVersion,
            PromptVersion = r.PromptVersion,
            RetrievalCount = r.RetrievalCount,
            CitationCount = r.CitationCount,
            LatencyMs = r.LatencyMs,
            CorrelationId = r.CorrelationId,
            Citations = r.Citations.Select(c => new TutorCitationDto
            {
                SourceDocumentId = c.SourceDocumentId,
                ChunkId = c.ChunkId,
                Score = c.Score,
                Title = c.Title,
                Grade = c.Grade,
                Subject = c.Subject,
                Unit = c.Unit,
                Lesson = c.Lesson,
                Snippet = c.Snippet,
            }).ToList(),
        };
    }
}
