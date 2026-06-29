using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Ai;
using DerasaX.Domain.Exceptions;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 6 §15 — deterministic resilience tests for the AI client pipeline
/// (bounded retry + jitter + circuit breaker). No wall-clock waits: the delay
/// function is a no-op and the breaker clock is injected.
/// </summary>
public class AiResilienceTests
{
    private static AiResilienceSettings Settings(int retries = 2, int threshold = 3, int resetSeconds = 30) => new()
    {
        MaxRetries = retries,
        BaseDelayMilliseconds = 1,
        MaxDelayMilliseconds = 2,
        CircuitFailureThreshold = threshold,
        CircuitResetSeconds = resetSeconds,
    };

    private static AiResiliencePipeline Pipeline(AiResilienceSettings s, AiCircuitBreaker breaker) =>
        new(s, breaker, delay: (_, _) => Task.CompletedTask);

    private static HttpResponseMessage Resp(HttpStatusCode code) => new(code);

    // ---- Retry eligibility / exclusion / exhaustion -------------------------

    [Fact]
    public async Task Retries_transient_5xx_then_succeeds()
    {
        var s = Settings();
        var pipe = Pipeline(s, new AiCircuitBreaker(s));
        var calls = 0;
        var res = await pipe.ExecuteAsync(_ =>
        {
            calls++;
            return Task.FromResult(Resp(calls < 2 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK));
        }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(2, calls); // one failure, one success
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData((HttpStatusCode)422)]
    public async Task Does_not_retry_deterministic_4xx(HttpStatusCode code)
    {
        var s = Settings();
        var breaker = new AiCircuitBreaker(s);
        var pipe = Pipeline(s, breaker);
        var calls = 0;
        var res = await pipe.ExecuteAsync(_ => { calls++; return Task.FromResult(Resp(code)); }, CancellationToken.None);

        Assert.Equal(code, res.StatusCode);
        Assert.Equal(1, calls);                       // never retried
        Assert.Equal(AiCircuitBreaker.CircuitState.Closed, breaker.State); // never tripped breaker
    }

    [Fact]
    public async Task Retry_exhaustion_returns_last_response_and_records_failure()
    {
        var s = Settings(retries: 2, threshold: 100); // high threshold so breaker stays closed
        var breaker = new AiCircuitBreaker(s);
        var pipe = Pipeline(s, breaker);
        var calls = 0;
        var res = await pipe.ExecuteAsync(_ => { calls++; return Task.FromResult(Resp(HttpStatusCode.BadGateway)); }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadGateway, res.StatusCode);
        Assert.Equal(3, calls); // 1 + 2 retries
    }

    // ---- Timeout / cancellation --------------------------------------------

    [Fact]
    public async Task Timeout_is_retried_then_throws()
    {
        var s = Settings(retries: 1, threshold: 100);
        var pipe = Pipeline(s, new AiCircuitBreaker(s));
        var calls = 0;
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            pipe.ExecuteAsync(_ => { calls++; throw new TaskCanceledException("timeout"); }, CancellationToken.None));
        Assert.Equal(2, calls); // 1 + 1 retry
    }

    [Fact]
    public async Task Genuine_cancellation_is_not_retried()
    {
        var s = Settings();
        var pipe = Pipeline(s, new AiCircuitBreaker(s));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var calls = 0;
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            pipe.ExecuteAsync(ctx => { calls++; ctx.ThrowIfCancellationRequested(); return Task.FromResult(Resp(HttpStatusCode.OK)); }, cts.Token));
        Assert.Equal(1, calls); // not retried
    }

    [Fact]
    public async Task Transport_failure_is_retried_then_throws()
    {
        var s = Settings(retries: 2, threshold: 100);
        var pipe = Pipeline(s, new AiCircuitBreaker(s));
        var calls = 0;
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            pipe.ExecuteAsync(_ => { calls++; throw new HttpRequestException("dns"); }, CancellationToken.None));
        Assert.Equal(3, calls);
    }

    // ---- Circuit opening / recovery ----------------------------------------

    [Fact]
    public async Task Circuit_opens_after_threshold_and_fast_fails()
    {
        var s = Settings(retries: 0, threshold: 3, resetSeconds: 30);
        var breaker = new AiCircuitBreaker(s);
        var pipe = Pipeline(s, breaker);

        // 3 failing calls (no retries) → 3 recorded failures → open.
        for (var i = 0; i < 3; i++)
            await pipe.ExecuteAsync(_ => Task.FromResult(Resp(HttpStatusCode.ServiceUnavailable)), CancellationToken.None);

        Assert.Equal(AiCircuitBreaker.CircuitState.Open, breaker.State);

        // Next call must fast-fail with circuit_open WITHOUT invoking send.
        var invoked = false;
        var ex = await Assert.ThrowsAsync<AiServiceException>(() =>
            pipe.ExecuteAsync(_ => { invoked = true; return Task.FromResult(Resp(HttpStatusCode.OK)); }, CancellationToken.None));
        Assert.Equal("circuit_open", ex.Category);
        Assert.False(invoked);
    }

    [Fact]
    public async Task Circuit_recovers_to_half_open_then_closes_on_success()
    {
        var s = Settings(retries: 0, threshold: 2, resetSeconds: 10);
        var now = DateTimeOffset.UtcNow;
        var breaker = new AiCircuitBreaker(s, clock: () => now);
        var pipe = Pipeline(s, breaker);

        // Trip the breaker open.
        for (var i = 0; i < 2; i++)
            await pipe.ExecuteAsync(_ => Task.FromResult(Resp(HttpStatusCode.BadGateway)), CancellationToken.None);
        Assert.Equal(AiCircuitBreaker.CircuitState.Open, breaker.State);

        // Advance past the reset window → HALF-OPEN admits a trial.
        now = now.AddSeconds(11);
        Assert.Equal(AiCircuitBreaker.CircuitState.HalfOpen, breaker.State);

        var res = await pipe.ExecuteAsync(_ => Task.FromResult(Resp(HttpStatusCode.OK)), CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(AiCircuitBreaker.CircuitState.Closed, breaker.State); // success closed it
    }

    [Fact]
    public async Task Half_open_trial_failure_reopens_circuit()
    {
        var s = Settings(retries: 0, threshold: 2, resetSeconds: 10);
        var now = DateTimeOffset.UtcNow;
        var breaker = new AiCircuitBreaker(s, clock: () => now);
        var pipe = Pipeline(s, breaker);

        for (var i = 0; i < 2; i++)
            await pipe.ExecuteAsync(_ => Task.FromResult(Resp(HttpStatusCode.BadGateway)), CancellationToken.None);
        now = now.AddSeconds(11);
        Assert.Equal(AiCircuitBreaker.CircuitState.HalfOpen, breaker.State);

        // Trial fails → re-open.
        await pipe.ExecuteAsync(_ => Task.FromResult(Resp(HttpStatusCode.BadGateway)), CancellationToken.None);
        Assert.Equal(AiCircuitBreaker.CircuitState.Open, breaker.State);
    }

    [Fact]
    public async Task Success_resets_failure_count()
    {
        var s = Settings(retries: 0, threshold: 3);
        var breaker = new AiCircuitBreaker(s);
        var pipe = Pipeline(s, breaker);

        await pipe.ExecuteAsync(_ => Task.FromResult(Resp(HttpStatusCode.ServiceUnavailable)), CancellationToken.None);
        await pipe.ExecuteAsync(_ => Task.FromResult(Resp(HttpStatusCode.ServiceUnavailable)), CancellationToken.None);
        await pipe.ExecuteAsync(_ => Task.FromResult(Resp(HttpStatusCode.OK)), CancellationToken.None); // resets
        // Two more failures should NOT open (count was reset).
        await pipe.ExecuteAsync(_ => Task.FromResult(Resp(HttpStatusCode.ServiceUnavailable)), CancellationToken.None);
        await pipe.ExecuteAsync(_ => Task.FromResult(Resp(HttpStatusCode.ServiceUnavailable)), CancellationToken.None);

        Assert.Equal(AiCircuitBreaker.CircuitState.Closed, breaker.State);
    }
}
