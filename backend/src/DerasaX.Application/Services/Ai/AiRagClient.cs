using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.AiDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Ai;
using DerasaX.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace DerasaX.Application.Services.Ai
{
    /// <summary>
    /// HTTP implementation of <see cref="IAiRagClient"/>. The base address and
    /// timeout are configured on the injected <see cref="HttpClient"/> (typed
    /// client). The JSON contract uses snake_case to match the Python schema.
    ///
    /// All requests flow through a shared <see cref="AiResiliencePipeline"/>
    /// (bounded retry + circuit breaker, §15). Every transport/timeout/non-2xx
    /// outcome is mapped to a stable <see cref="AiServiceException"/> whose
    /// <c>Category</c> classifies the failure for telemetry; the provider body is
    /// never leaked to the caller. A fresh <see cref="HttpRequestMessage"/> is
    /// built per attempt so retries are safe.
    /// </summary>
    public class AiRagClient : IAiRagClient
    {
        private const string TutorScope = "ai:tutor";
        private const string IngestScope = "ai:ingest";
        private const string QuizScope = "ai:quiz";
        private const string PredictionScope = "ai:prediction";
        private const string AnalysisScope = "ai:analyze";
        private const string VisionScope = "ai:vision";

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
        };

        private readonly HttpClient _http;
        private readonly IAiServiceTokenProvider _tokens;
        private readonly ILogger<AiRagClient> _logger;
        private readonly AiResiliencePipeline _resilience;

        public AiRagClient(HttpClient http, IAiServiceTokenProvider tokens, ILogger<AiRagClient> logger, AiResiliencePipeline resilience)
        {
            _http = http;
            _tokens = tokens;
            _logger = logger;
            _resilience = resilience;
        }

        public Task<AiTutorResponse> TutorAsync(AiTutorRequest request, string tenantId, string? actorUserId, CancellationToken ct = default)
            => SendAsync<AiTutorResponse>(HttpMethod.Post, "/internal/v1/tutor", TutorScope, tenantId, actorUserId, request.CorrelationId, request, ct);

        public Task<AiQuizDraftResponse> QuizDraftAsync(AiQuizDraftRequest request, string tenantId, string? actorUserId, CancellationToken ct = default)
            => SendAsync<AiQuizDraftResponse>(HttpMethod.Post, "/internal/v1/quiz/draft", QuizScope, tenantId, actorUserId, request.CorrelationId, request, ct);

        public Task<AiPredictionResponse> PredictAsync(AiPredictionRequest request, string tenantId, string? actorUserId, CancellationToken ct = default)
            => SendAsync<AiPredictionResponse>(HttpMethod.Post, "/internal/v1/prediction", PredictionScope, tenantId, actorUserId, request.CorrelationId, request, ct);

        public Task<AiAnalysisResponse> AnalyzeAsync(AiAnalysisRequest request, string tenantId, string? actorUserId, CancellationToken ct = default)
            => SendAsync<AiAnalysisResponse>(HttpMethod.Post, "/internal/v1/analysis", AnalysisScope, tenantId, actorUserId, request.CorrelationId, request, ct);

        public Task<AiIngestResponse> IngestDocumentAsync(AiIngestRequest request, string tenantId, string? actorUserId, CancellationToken ct = default)
            => SendAsync<AiIngestResponse>(HttpMethod.Post, "/internal/v1/documents", IngestScope, tenantId, actorUserId, request.CorrelationId, request, ct);

        public Task<AiVisionAnalyzeResponse> AnalyzeVisionFrameAsync(AiVisionAnalyzeRequest request, string tenantId, string? actorUserId, CancellationToken ct = default)
            => SendAsync<AiVisionAnalyzeResponse>(HttpMethod.Post, "/internal/v1/vision/analyze", VisionScope, tenantId, actorUserId, request.CorrelationId, request, ct);

        public Task<AiVisionEndSessionResponse> EndVisionSessionAsync(AiVisionEndSessionRequest request, string tenantId, string? actorUserId, CancellationToken ct = default)
            => SendAsync<AiVisionEndSessionResponse>(HttpMethod.Post, "/internal/v1/vision/end-session", VisionScope, tenantId, actorUserId, request.CorrelationId, request, ct);

        public Task<AiDeleteResponse> DeleteDocumentAsync(string documentId, string correlationId, string tenantId, string? actorUserId, CancellationToken ct = default)
            => SendAsync<AiDeleteResponse>(HttpMethod.Delete, $"/internal/v1/documents/{Uri.EscapeDataString(documentId)}", IngestScope, tenantId, actorUserId, correlationId, body: null, ct);

        /// <summary>
        /// Shared request pipeline: mint a fresh scoped token, send through the
        /// resilience pipeline (a new request message per attempt), and map any
        /// transport/timeout/non-2xx outcome to a stable
        /// <see cref="AiServiceException"/> (never leaking the internal/provider body).
        /// </summary>
        private async Task<TResponse> SendAsync<TResponse>(
            HttpMethod method, string path, string scope, string tenantId, string? actorUserId,
            string correlationId, object? body, CancellationToken ct)
        {
            var token = _tokens.CreateToken(tenantId, actorUserId, scope);

            HttpResponseMessage res;
            try
            {
                res = await _resilience.ExecuteAsync(attemptCt =>
                {
                    var msg = new HttpRequestMessage(method, path);
                    msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
                    if (!string.IsNullOrEmpty(correlationId))
                        msg.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
                    if (body is not null)
                        msg.Content = JsonContent.Create(body, body.GetType(), options: Json);
                    return _http.SendAsync(msg, attemptCt);
                }, ct);
            }
            catch (AiServiceException ex)
            {
                // Circuit open (fast-fail) — already a safe, shaped failure.
                _logger.LogWarning("AI call short-circuited. path={Path} category={Category} correlationId={CorrelationId}",
                    path, ex.Category, correlationId);
                throw;
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("AI call timed out. path={Path} correlationId={CorrelationId}", path, correlationId);
                throw new AiServiceException("timeout", "The AI service did not respond in time.", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "AI call transport failure. path={Path} correlationId={CorrelationId}", path, correlationId);
                throw new AiServiceException("unavailable", "The AI service is unavailable.", ex);
            }

            if (!res.IsSuccessStatusCode)
            {
                var category = MapStatus(res.StatusCode);
                _logger.LogWarning("AI call failed. path={Path} status={Status} category={Category} correlationId={CorrelationId}",
                    path, (int)res.StatusCode, category, correlationId);
                res.Dispose();
                throw new AiServiceException(category, "The AI service returned an error.");
            }

            TResponse? parsed;
            try
            {
                parsed = await res.Content.ReadFromJsonAsync<TResponse>(Json, ct);
            }
            catch (JsonException ex)
            {
                throw new AiServiceException("bad_response", "The AI service returned an invalid response.", ex);
            }
            finally
            {
                res.Dispose();
            }

            if (parsed is null)
                throw new AiServiceException("bad_response", "The AI service returned an empty response.");

            return parsed;
        }

        /// <summary>Classify a non-2xx AI response status for telemetry (no body leaked).</summary>
        private static string MapStatus(HttpStatusCode code) => code switch
        {
            HttpStatusCode.Unauthorized => "provider_unauthorized",   // 401
            HttpStatusCode.Forbidden => "provider_forbidden",         // 403
            HttpStatusCode.BadRequest => "provider_rejected",         // 400
            (HttpStatusCode)422 => "provider_rejected",               // Unprocessable Entity
            HttpStatusCode.NotFound => "provider_not_found",          // 404
            _ => "provider_error",                                    // 408/429/5xx + anything else
        };
    }
}
