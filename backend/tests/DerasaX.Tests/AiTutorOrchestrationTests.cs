using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.AiDto;
using DerasaX.Application.Dto.OperationsDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Ai;
using DerasaX.Application.Services.Abstractions.Operations;
using DerasaX.Application.Services.Ai;
using DerasaX.Domain.Common;
using DerasaX.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 6 §9/§20 — backend AI tutor orchestration + internal-contract client.
/// Pure unit tests (no DB, no live AI): fakes for the tenant context, AI client,
/// and usage recorder; a stub HttpMessageHandler for the wire-contract test.
/// </summary>
public class AiTutorOrchestrationTests
{
    // ----- Fakes -------------------------------------------------------------

    private sealed class FakeTenant : ITenantContext
    {
        public string? TenantId { get; init; } = "tenant-1";
        public string? UserId { get; init; } = "stu-1";
        public string? Role { get; init; } = "Student";
        public bool HasTenant => !string.IsNullOrEmpty(TenantId);
        public bool IsPlatformScope => false;
        public bool IsAuthenticated => true;
    }

    private sealed class FakeUsage : IAiUsageService
    {
        public readonly List<RecordAiUsageDto> Recorded = new();
        public Task<AiUsageDto> RecordInternalAsync(RecordAiUsageDto dto, CancellationToken ct = default)
        {
            Recorded.Add(dto);
            return Task.FromResult(new AiUsageDto { Id = "u1", Kind = dto.Kind, Provider = dto.Provider });
        }
        public Task<ApiResponse<AiUsageDto>> RecordAsync(RecordAiUsageDto dto, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<PaginationResponse<IEnumerable<AiUsageDto>>> ListAsync(AiUsageParameters p, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<ApiResponse<AiUsageSummaryDto>> SummaryAsync(CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeAiClient : IAiRagClient
    {
        public Task<AiVisionAnalyzeResponse> AnalyzeVisionFrameAsync(AiVisionAnalyzeRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AiVisionEndSessionResponse> EndVisionSessionAsync(AiVisionEndSessionRequest r, string t, string? u, CancellationToken ct = default) => throw new NotImplementedException();
        public AiTutorRequest? LastRequest;
        public string? LastTenant;
        public Func<AiTutorResponse>? OnCall;
        public Exception? Throw;

        public AiIngestRequest? LastIngest;
        public Func<AiIngestResponse>? OnIngest = null;

        public Task<AiTutorResponse> TutorAsync(AiTutorRequest request, string tenantId, string? actorUserId, CancellationToken ct = default)
        {
            LastRequest = request;
            LastTenant = tenantId;
            if (Throw is not null) throw Throw;
            return Task.FromResult(OnCall?.Invoke() ?? new AiTutorResponse { Answer = "ok", Grounded = true });
        }

        public Task<AiIngestResponse> IngestDocumentAsync(AiIngestRequest request, string tenantId, string? actorUserId, CancellationToken ct = default)
        {
            LastIngest = request;
            LastTenant = tenantId;
            if (Throw is not null) throw Throw;
            return Task.FromResult(OnIngest?.Invoke() ?? new AiIngestResponse
            {
                DocumentId = request.DocumentId, Version = request.Version, Status = "indexed",
                ChunkCount = 3, Checksum = "abc", Language = request.Language, CorrelationId = request.CorrelationId,
            });
        }

        public Task<AiDeleteResponse> DeleteDocumentAsync(string documentId, string correlationId, string tenantId, string? actorUserId, CancellationToken ct = default)
        {
            LastTenant = tenantId;
            return Task.FromResult(new AiDeleteResponse { DocumentId = documentId, DeletedChunks = 2, Status = "deleted" });
        }

        public AiQuizDraftRequest? LastQuiz;
        public Func<AiQuizDraftResponse>? OnQuiz = null;

        public Task<AiQuizDraftResponse> QuizDraftAsync(AiQuizDraftRequest request, string tenantId, string? actorUserId, CancellationToken ct = default)
        {
            LastQuiz = request;
            LastTenant = tenantId;
            if (Throw is not null) throw Throw;
            return Task.FromResult(OnQuiz?.Invoke() ?? new AiQuizDraftResponse { Grounded = true, Draft = new AiQuizDraft { QuestionCount = 0 } });
        }

        public Task<AiPredictionResponse> PredictAsync(AiPredictionRequest request, string tenantId, string? actorUserId, CancellationToken ct = default)
        {
            LastTenant = tenantId;
            if (Throw is not null) throw Throw;
            return Task.FromResult(new AiPredictionResponse { PredictionType = "performance", Score = 75, Level = "Medium", RiskBand = "medium" });
        }

        public AiAnalysisRequest? LastAnalysis;
        public Func<AiAnalysisResponse>? OnAnalysis = null;

        public Task<AiAnalysisResponse> AnalyzeAsync(AiAnalysisRequest request, string tenantId, string? actorUserId, CancellationToken ct = default)
        {
            LastAnalysis = request;
            LastTenant = tenantId;
            if (Throw is not null) throw Throw;
            return Task.FromResult(OnAnalysis?.Invoke() ?? new AiAnalysisResponse
            {
                PainPointCategory = "concept", EvidenceSummary = "struggles with fractions", Recommendation = "practice",
                Confidence = 0.7, EscalationLevel = "monitor", HumanReviewRequired = true,
                Model = "m", ModelVersion = "m-v1", PromptVersion = "analysis.v1", CorrelationId = request.CorrelationId,
            });
        }
    }

    private static AiTutorResponse GroundedResponse() => new()
    {
        Answer = "Photosynthesis converts light to energy.",
        Grounded = true,
        Provider = "groq",
        Model = "llama-3.1-8b-instant",
        ModelVersion = "llama-3.1-8b-instant",
        PromptVersion = "tutor.v1",
        RetrievalCount = 2,
        CitationCount = 1,
        LatencyMs = 42,
        CorrelationId = "corr-x",
        Citations = new() { new AiTutorCitation { SourceDocumentId = "d1", ChunkId = "d1-c", Score = 0.9 } },
    };

    private static TutorService NewService(FakeAiClient ai, FakeUsage usage, ITenantContext? tenant = null) =>
        new(ai, tenant ?? new FakeTenant(), usage, NullLogger<TutorService>.Instance);

    // ----- TutorService ------------------------------------------------------

    [Fact]
    public async Task Successful_tutor_returns_normalized_response_and_records_usage()
    {
        var ai = new FakeAiClient { OnCall = GroundedResponse };
        var usage = new FakeUsage();
        var svc = NewService(ai, usage);

        var res = await svc.AskAsync(new TutorChatRequestDto { Message = "What is photosynthesis?", Grade = 8, Subject = "science" });

        Assert.True(res.Grounded);
        Assert.Equal("tutor.v1", res.PromptVersion);
        Assert.Single(res.Citations);
        Assert.Equal("d1", res.Citations[0].SourceDocumentId);
        // tenant came from context, not body
        Assert.Equal("tenant-1", ai.LastTenant);
        Assert.Equal(8, ai.LastRequest!.Grade);
        // usage recorded, not failed
        var rec = Assert.Single(usage.Recorded);
        Assert.False(rec.Failed);
        Assert.Equal(DerasaX.Domain.Enums.AiUsageKind.Chat, rec.Kind);
    }

    [Fact]
    public async Task Provider_failure_records_failed_usage_and_rethrows()
    {
        var ai = new FakeAiClient { Throw = new AiServiceException("timeout", "The AI service did not respond in time.") };
        var usage = new FakeUsage();
        var svc = NewService(ai, usage);

        await Assert.ThrowsAsync<AiServiceException>(() =>
            svc.AskAsync(new TutorChatRequestDto { Message = "hi" }));

        var rec = Assert.Single(usage.Recorded);
        Assert.True(rec.Failed);
    }

    [Fact]
    public async Task Missing_tenant_is_rejected()
    {
        var svc = NewService(new FakeAiClient(), new FakeUsage(), new FakeTenant { TenantId = null });
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            svc.AskAsync(new TutorChatRequestDto { Message = "hi" }));
    }

    [Fact]
    public async Task Empty_message_is_rejected()
    {
        var svc = NewService(new FakeAiClient(), new FakeUsage());
        await Assert.ThrowsAsync<BadRequestException>(() =>
            svc.AskAsync(new TutorChatRequestDto { Message = "   " }));
    }

    [Fact]
    public async Task History_is_bounded_and_roles_normalized()
    {
        var ai = new FakeAiClient { OnCall = GroundedResponse };
        var svc = NewService(ai, new FakeUsage());

        var history = Enumerable.Range(0, 30)
            .Select(i => new TutorTurnDto { Role = i % 2 == 0 ? "weird-role" : "assistant", Content = $"m{i}" })
            .ToList();

        await svc.AskAsync(new TutorChatRequestDto { Message = "next", History = history });

        Assert.True(ai.LastRequest!.History.Count <= 20);
        Assert.All(ai.LastRequest.History, t => Assert.Contains(t.Role, new[] { "user", "assistant" }));
    }

    // ----- AiRagClient wire contract ----------------------------------------

    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request;
        public string? RequestBody;
        public Func<HttpResponseMessage>? Responder;
        public Exception? Throw;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            if (request.Content is not null)
                RequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            if (Throw is not null) throw Throw;
            return Responder?.Invoke() ?? new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class FakeTokens : IAiServiceTokenProvider
    {
        public string? LastScope;
        public string? LastTenant;
        public ServiceTokenResult CreateToken(string? tenantId, string? actorUserId, string scope)
        {
            LastScope = scope; LastTenant = tenantId;
            return new ServiceTokenResult { Token = "test.jwt.token", Issuer = "derasax-backend", Audience = "school-ai-rag" };
        }
    }

    // A fast resilience pipeline for wire-contract tests: no real backoff delay,
    // so retried failures don't slow the suite. Default retry/circuit settings.
    internal static AiResiliencePipeline FastPipeline(DerasaX.Application.Common.AiResilienceSettings? s = null)
    {
        var settings = s ?? new DerasaX.Application.Common.AiResilienceSettings();
        return new AiResiliencePipeline(settings, new AiCircuitBreaker(settings), delay: (_, _) => Task.CompletedTask);
    }

    private static AiRagClient NewClient(StubHandler handler, FakeTokens tokens) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") }, tokens, NullLogger<AiRagClient>.Instance, FastPipeline());

    [Fact]
    public async Task Client_posts_snake_case_body_with_bearer_token()
    {
        var handler = new StubHandler
        {
            Responder = () => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    answer = "A",
                    grounded = true,
                    no_answer_reason = (string?)null,
                    citations = new[] { new { source_document_id = "d1", chunk_id = "d1-c", score = 0.9 } },
                    provider = "groq",
                    model = "m",
                    model_version = "m-v1",
                    prompt_version = "tutor.v1",
                    retrieval_count = 2,
                    citation_count = 1,
                    latency_ms = 10,
                    correlation_id = "corr-1",
                }),
            },
        };
        var tokens = new FakeTokens();
        var client = NewClient(handler, tokens);

        var resp = await client.TutorAsync(new AiTutorRequest { CorrelationId = "corr-1", Message = "q", TopK = 5 }, "tenant-1", "stu-1");

        Assert.Equal("/internal/v1/tutor", handler.Request!.RequestUri!.AbsolutePath);
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal("test.jwt.token", handler.Request.Headers.Authorization.Parameter);
        Assert.Equal("ai:tutor", tokens.LastScope);
        Assert.Equal("tenant-1", tokens.LastTenant);
        // snake_case wire fields
        Assert.Contains("\"correlation_id\"", handler.RequestBody);
        Assert.Contains("\"top_k\"", handler.RequestBody);
        // parsed response
        Assert.True(resp.Grounded);
        Assert.Equal("tutor.v1", resp.PromptVersion);
        Assert.Single(resp.Citations);
        Assert.Equal("d1", resp.Citations[0].SourceDocumentId);
    }

    [Fact]
    public async Task Client_maps_error_status_to_ai_service_exception()
    {
        var handler = new StubHandler { Responder = () => new HttpResponseMessage(HttpStatusCode.InternalServerError) };
        var client = NewClient(handler, new FakeTokens());

        var ex = await Assert.ThrowsAsync<AiServiceException>(() =>
            client.TutorAsync(new AiTutorRequest { CorrelationId = "c", Message = "q" }, "tenant-1", null));
        Assert.Equal("provider_error", ex.Category);
    }

    [Fact]
    public async Task Client_maps_timeout_to_ai_service_exception()
    {
        var handler = new StubHandler { Throw = new TaskCanceledException("timeout") };
        var client = NewClient(handler, new FakeTokens());

        var ex = await Assert.ThrowsAsync<AiServiceException>(() =>
            client.TutorAsync(new AiTutorRequest { CorrelationId = "c", Message = "q" }, "tenant-1", null));
        Assert.Equal("timeout", ex.Category);
    }

    // ----- Document ingestion orchestration (§7) -----------------------------

    private static AiDocumentService NewDocSvc(FakeAiClient ai, ITenantContext? tenant = null) =>
        new(ai, tenant ?? new FakeTenant(), NullLogger<AiDocumentService>.Instance);

    [Fact]
    public async Task Ingest_forwards_tenant_from_context_and_returns_result()
    {
        var ai = new FakeAiClient();
        var svc = NewDocSvc(ai);
        var res = await svc.IngestAsync(new IngestCurriculumDocumentDto
        {
            DocumentId = "d1", Version = 1, Content = "some curriculum text",
            Language = "ar", Subject = "Science", Grade = 8, MaterialType = "textbook",
        });
        Assert.Equal("indexed", res.Status);
        Assert.Equal("tenant-1", ai.LastTenant);          // tenant from context, not body
        Assert.Equal("ar", ai.LastIngest!.Language);
        Assert.Equal("d1", ai.LastIngest.DocumentId);
        Assert.False(string.IsNullOrEmpty(ai.LastIngest.CorrelationId));
    }

    [Fact]
    public async Task Ingest_missing_tenant_rejected()
    {
        var svc = NewDocSvc(new FakeAiClient(), new FakeTenant { TenantId = null });
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            svc.IngestAsync(new IngestCurriculumDocumentDto { DocumentId = "d1", Content = "x" }));
    }

    [Fact]
    public async Task Ingest_empty_content_rejected()
    {
        var svc = NewDocSvc(new FakeAiClient());
        await Assert.ThrowsAsync<BadRequestException>(() =>
            svc.IngestAsync(new IngestCurriculumDocumentDto { DocumentId = "d1", Content = "   " }));
    }

    [Fact]
    public async Task Delete_returns_mapped_result()
    {
        var res = await NewDocSvc(new FakeAiClient()).DeleteAsync("d1");
        Assert.Equal("deleted", res.Status);
        Assert.Equal(2, res.DeletedChunks);
    }

    [Fact]
    public async Task Client_ingest_posts_snake_case_to_documents_with_ingest_scope()
    {
        var handler = new StubHandler
        {
            Responder = () => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    document_id = "d1", version = 1, status = "indexed", chunk_count = 2,
                    removed_chunks = 0, checksum = "abc", language = "en", collection = "t_x",
                    indexed_at = "now", correlation_id = "c1",
                }),
            },
        };
        var tokens = new FakeTokens();
        var client = NewClient(handler, tokens);

        var resp = await client.IngestDocumentAsync(
            new AiIngestRequest { CorrelationId = "c1", DocumentId = "d1", Version = 1, Content = "text", MaterialType = "textbook" },
            "tenant-1", "u1");

        Assert.Equal("/internal/v1/documents", handler.Request!.RequestUri!.AbsolutePath);
        Assert.Equal("ai:ingest", tokens.LastScope);
        Assert.Contains("\"document_id\"", handler.RequestBody);
        Assert.Contains("\"material_type\"", handler.RequestBody);
        Assert.Equal("indexed", resp.Status);
        Assert.Equal(2, resp.ChunkCount);
    }

    [Fact]
    public async Task Client_delete_sends_delete_to_document_path()
    {
        var handler = new StubHandler
        {
            Responder = () => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { document_id = "d1", deleted_chunks = 3, status = "deleted" }),
            },
        };
        var client = NewClient(handler, new FakeTokens());
        var resp = await client.DeleteDocumentAsync("d1", "c1", "tenant-1", null);

        Assert.Equal(HttpMethod.Delete, handler.Request!.Method);
        Assert.Equal("/internal/v1/documents/d1", handler.Request.RequestUri!.AbsolutePath);
        Assert.Equal("deleted", resp.Status);
        Assert.Equal(3, resp.DeletedChunks);
    }

    [Fact]
    public async Task Client_prediction_posts_snake_case_with_prediction_scope()
    {
        var handler = new StubHandler
        {
            Responder = () => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    student_ref = "s", prediction_type = "performance", score = 80.0, level = "Strong",
                    risk_band = "low", confidence = 0.9, factors = Array.Empty<object>(), model_name = "rf-performance",
                    model_version = "rf-2026.06", feature_schema_version = "perf-v1", generated_at = "now",
                    limitations = new[] { "Advisory only." }, correlation_id = "c1",
                }),
            },
        };
        var tokens = new FakeTokens();
        var client = NewClient(handler, tokens);

        var resp = await client.PredictAsync(new AiPredictionRequest
        {
            CorrelationId = "c1", StudentRef = "s", FeatureSchemaVersion = "perf-v1",
            Features = new AiPredictionFeatures { Age = 14, StudyHours = 12, AttendancePercentage = 85, Gender = "male", SchoolType = "public", InternetAccess = "yes", TravelTime = "<15 min", ExtraActivities = "no", StudyMethod = "textbook" },
        }, "tenant-1", "u1");

        Assert.Equal("/internal/v1/prediction", handler.Request!.RequestUri!.AbsolutePath);
        Assert.Equal("ai:prediction", tokens.LastScope);
        Assert.Contains("\"feature_schema_version\"", handler.RequestBody);
        Assert.Contains("\"attendance_percentage\"", handler.RequestBody);
        Assert.Equal("Strong", resp.Level);
        Assert.Equal("rf-2026.06", resp.ModelVersion);
    }

    [Fact]
    public async Task Client_quiz_draft_posts_snake_case_with_quiz_scope_and_parses_response()
    {
        var handler = new StubHandler
        {
            Responder = () => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    draft = new
                    {
                        title = "Quiz", instructions = "Answer.", grade = 8, subject = "Science",
                        difficulty = "core", question_count = 1,
                        questions = new[]
                        {
                            new { question_type = "mcq", question_text = "Q?", options = new[] { "A", "B", "C", "D" },
                                  correct_index = 0, explanation = "e", points = 2, source_references = new[] { "doc-1" } }
                        }
                    },
                    grounded = true,
                    citations = new[] { new { source_document_id = "doc-1", chunk_id = "c", score = 0.9 } },
                    provider = "groq", model = "m", model_version = "m-v1", prompt_version = "quiz.v1",
                    retrieval_count = 1, correlation_id = "c1", generated_at = "now",
                }),
            },
        };
        var tokens = new FakeTokens();
        var client = NewClient(handler, tokens);

        var resp = await client.QuizDraftAsync(
            new AiQuizDraftRequest { CorrelationId = "c1", NumQuestions = 1, Subject = "Science", TopK = 6 },
            "tenant-1", "u1");

        Assert.Equal("/internal/v1/quiz/draft", handler.Request!.RequestUri!.AbsolutePath);
        Assert.Equal("ai:quiz", tokens.LastScope);
        Assert.Contains("\"num_questions\"", handler.RequestBody);
        Assert.Contains("\"question_types\"", handler.RequestBody);
        Assert.True(resp.Grounded);
        Assert.Equal("quiz.v1", resp.PromptVersion);
        Assert.Single(resp.Draft.Questions);
        Assert.Equal(0, resp.Draft.Questions[0].CorrectIndex);
    }
}
