using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.AiDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Ai;
using DerasaX.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 6 §20 — wire-level integration of the AI client with the resilience
/// pipeline + service-token provider (a counting/scripted HttpMessageHandler, no
/// live AI). Proves: correlation propagation, scope-specific tokens, granular
/// error mapping, transient retry, and circuit-open fast-fail end-to-end through
/// the real AiRagClient.SendAsync path.
/// </summary>
public class AiClientContractTests
{
    private sealed class ScriptHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> _script;
        public int Calls;
        public HttpRequestMessage? LastRequest;
        public string? LastCorrelation;

        public ScriptHandler(params Func<HttpResponseMessage>[] steps) => _script = new(steps);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            LastRequest = request;
            if (request.Headers.TryGetValues("X-Correlation-Id", out var v))
                LastCorrelation = string.Join(",", v);
            var step = _script.Count > 1 ? _script.Dequeue() : _script.Peek();
            return Task.FromResult(step());
        }
    }

    private sealed class FakeTokens : IAiServiceTokenProvider
    {
        public string? LastScope;
        public string? LastTenant;
        public string? LastActor;
        public ServiceTokenResult CreateToken(string? tenantId, string? actorUserId, string scope)
        {
            LastScope = scope; LastTenant = tenantId; LastActor = actorUserId;
            return new ServiceTokenResult { Token = "test.jwt.token", Issuer = "derasax-backend", Audience = "school-ai-rag" };
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode code) =>
        new(code)
        {
            Content = System.Net.Http.Json.JsonContent.Create(new
            {
                answer = "A", grounded = true, no_answer_reason = (string?)null,
                citations = Array.Empty<object>(), provider = "groq", model = "m", model_version = "m-v1",
                prompt_version = "tutor.v1", retrieval_count = 0, citation_count = 0, latency_ms = 1, correlation_id = "c",
            }),
        };

    private static AiRagClient Client(ScriptHandler handler, FakeTokens tokens, AiResilienceSettings? s = null)
    {
        var settings = s ?? new AiResilienceSettings { MaxRetries = 2, CircuitFailureThreshold = 3, CircuitResetSeconds = 30 };
        var pipe = new AiResiliencePipeline(settings, new AiCircuitBreaker(settings), delay: (_, _) => Task.CompletedTask);
        return new AiRagClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") },
            tokens, NullLogger<AiRagClient>.Instance, pipe);
    }

    private static AiTutorRequest Req() => new() { CorrelationId = "corr-42", Message = "q", TopK = 4 };

    [Fact]
    public async Task Propagates_correlation_id_and_scope_specific_token()
    {
        var handler = new ScriptHandler(() => Json(HttpStatusCode.OK));
        var tokens = new FakeTokens();
        var client = Client(handler, tokens);

        await client.TutorAsync(Req(), "tenant-9", "user-7");

        Assert.Equal("corr-42", handler.LastCorrelation);   // correlation propagated to AI
        Assert.Equal("ai:tutor", tokens.LastScope);          // scope-specific token
        Assert.Equal("tenant-9", tokens.LastTenant);         // trusted tenant propagated
        Assert.Equal("user-7", tokens.LastActor);            // trusted user context propagated
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "provider_unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "provider_forbidden")]
    [InlineData(HttpStatusCode.BadRequest, "provider_rejected")]
    [InlineData((HttpStatusCode)422, "provider_rejected")]
    public async Task Maps_deterministic_4xx_to_categories_without_retry(HttpStatusCode code, string category)
    {
        var handler = new ScriptHandler(() => new HttpResponseMessage(code));
        var client = Client(handler, new FakeTokens());

        var ex = await Assert.ThrowsAsync<AiServiceException>(() => client.TutorAsync(Req(), "t", null));
        Assert.Equal(category, ex.Category);
        Assert.Equal(1, handler.Calls); // 4xx never retried
    }

    [Fact]
    public async Task Retries_transient_503_then_succeeds()
    {
        var handler = new ScriptHandler(
            () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            () => Json(HttpStatusCode.OK));
        var client = Client(handler, new FakeTokens());

        var resp = await client.TutorAsync(Req(), "t", null);
        Assert.True(resp.Grounded);
        Assert.Equal(2, handler.Calls); // retried once
    }

    [Fact]
    public async Task Circuit_opens_after_threshold_and_fast_fails()
    {
        var settings = new AiResilienceSettings { MaxRetries = 0, CircuitFailureThreshold = 2, CircuitResetSeconds = 30 };
        var handler = new ScriptHandler(() => new HttpResponseMessage(HttpStatusCode.BadGateway));
        var tokens = new FakeTokens();
        var pipe = new AiResiliencePipeline(settings, new AiCircuitBreaker(settings), delay: (_, _) => Task.CompletedTask);
        var client = new AiRagClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") },
            tokens, NullLogger<AiRagClient>.Instance, pipe);

        // Two failures trip the breaker (threshold 2).
        await Assert.ThrowsAsync<AiServiceException>(() => client.TutorAsync(Req(), "t", null));
        await Assert.ThrowsAsync<AiServiceException>(() => client.TutorAsync(Req(), "t", null));
        var callsBefore = handler.Calls;

        var ex = await Assert.ThrowsAsync<AiServiceException>(() => client.TutorAsync(Req(), "t", null));
        Assert.Equal("circuit_open", ex.Category);
        Assert.Equal(callsBefore, handler.Calls); // fast-fail: no new HTTP call
    }

    [Fact]
    public async Task Timeout_maps_to_timeout_category()
    {
        var handler = new ScriptHandler(() => throw new TaskCanceledException("timeout"));
        var settings = new AiResilienceSettings { MaxRetries = 0, CircuitFailureThreshold = 10 };
        var client = Client(handler, new FakeTokens(), settings);

        var ex = await Assert.ThrowsAsync<AiServiceException>(() => client.TutorAsync(Req(), "t", null));
        Assert.Equal("timeout", ex.Category);
    }
}
