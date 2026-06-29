using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Domain.Exceptions;

namespace DerasaX.Application.Services.Ai
{
    /// <summary>
    /// A small, dependency-free, thread-safe circuit breaker for the internal AI
    /// client (Phase 6 §15). It counts <em>consecutive provider failures</em>
    /// (timeout, transport, 408/429/5xx). After the configured threshold the
    /// circuit OPENS and fast-fails calls for a cooldown window; once the window
    /// elapses it becomes HALF-OPEN and admits a single trial call. A success
    /// closes it; a failure re-opens it.
    ///
    /// Deterministic 4xx responses (validation/auth/scope) are NOT counted as
    /// provider failures — they reflect the request, not provider health.
    ///
    /// The clock is injectable so circuit opening/recovery is unit-testable
    /// without wall-clock waits.
    /// </summary>
    public sealed class AiCircuitBreaker
    {
        public enum CircuitState { Closed, Open, HalfOpen }

        private readonly int _threshold;
        private readonly TimeSpan _resetAfter;
        private readonly Func<DateTimeOffset> _clock;
        private readonly object _lock = new();

        private int _consecutiveFailures;
        private DateTimeOffset? _openedAt;

        public AiCircuitBreaker(AiResilienceSettings settings, Func<DateTimeOffset>? clock = null)
        {
            _threshold = settings.CircuitFailureThreshold <= 0 ? 5 : settings.CircuitFailureThreshold;
            _resetAfter = TimeSpan.FromSeconds(settings.CircuitResetSeconds <= 0 ? 30 : settings.CircuitResetSeconds);
            _clock = clock ?? (() => DateTimeOffset.UtcNow);
        }

        public CircuitState State
        {
            get { lock (_lock) { return ComputeState(); } }
        }

        /// <summary>True only when the circuit is fully OPEN (fast-fail). HALF-OPEN admits a trial.</summary>
        public bool IsOpen => State == CircuitState.Open;

        private CircuitState ComputeState()
        {
            if (_openedAt is null) return CircuitState.Closed;
            return (_clock() - _openedAt.Value) >= _resetAfter
                ? CircuitState.HalfOpen
                : CircuitState.Open;
        }

        public void RecordSuccess()
        {
            lock (_lock)
            {
                _consecutiveFailures = 0;
                _openedAt = null;
            }
        }

        public void RecordFailure()
        {
            lock (_lock)
            {
                _consecutiveFailures++;
                if (_consecutiveFailures >= _threshold)
                    _openedAt = _clock();
            }
        }
    }

    /// <summary>
    /// Executes an HTTP send with bounded transient retries (exponential backoff
    /// + jitter) and the shared <see cref="AiCircuitBreaker"/> (Phase 6 §15).
    ///
    /// Retry policy:
    ///   * Retried: timeout (HttpClient.Timeout → TaskCanceledException without
    ///     caller cancellation), transport/DNS (HttpRequestException), and
    ///     transient status 408/429/500/502/503/504.
    ///   * NOT retried: deterministic 4xx (400/401/403/404/422 …) — returned to
    ///     the caller to map; genuine caller cancellation — propagated.
    ///
    /// The delay function is injectable so retry/backoff is unit-testable without
    /// real sleeps.
    /// </summary>
    public sealed class AiResiliencePipeline
    {
        private readonly AiResilienceSettings _settings;
        private readonly AiCircuitBreaker _breaker;
        private readonly Func<TimeSpan, CancellationToken, Task> _delay;
        private readonly Random _jitter = new();

        public AiResiliencePipeline(
            AiResilienceSettings settings,
            AiCircuitBreaker breaker,
            Func<TimeSpan, CancellationToken, Task>? delay = null)
        {
            _settings = settings;
            _breaker = breaker;
            _delay = delay ?? Task.Delay;
        }

        public AiCircuitBreaker Breaker => _breaker;

        public async Task<HttpResponseMessage> ExecuteAsync(
            Func<CancellationToken, Task<HttpResponseMessage>> send,
            CancellationToken ct)
        {
            if (_breaker.IsOpen)
                throw new AiServiceException("circuit_open", "The AI service is temporarily unavailable.");

            var maxRetries = _settings.MaxRetries < 0 ? 0 : _settings.MaxRetries;
            var attempt = 0;

            while (true)
            {
                attempt++;
                try
                {
                    var res = await send(ct).ConfigureAwait(false);

                    if (res.IsSuccessStatusCode)
                    {
                        _breaker.RecordSuccess();
                        return res;
                    }

                    // Deterministic client-side error: never retry, never trip the breaker.
                    if (!IsTransientStatus(res.StatusCode))
                        return res;

                    if (attempt <= maxRetries && !_breaker.IsOpen)
                    {
                        res.Dispose();
                        await _delay(Backoff(attempt), ct).ConfigureAwait(false);
                        continue;
                    }

                    _breaker.RecordFailure();
                    return res; // exhausted — caller maps to provider_error
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // Genuine caller cancellation — not a provider failure.
                    throw;
                }
                catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
                {
                    if (attempt <= maxRetries && !_breaker.IsOpen)
                    {
                        await _delay(Backoff(attempt), ct).ConfigureAwait(false);
                        continue;
                    }

                    _breaker.RecordFailure();
                    throw; // caller maps timeout/unavailable
                }
            }
        }

        private static bool IsTransientStatus(HttpStatusCode code) => code switch
        {
            HttpStatusCode.RequestTimeout => true,        // 408
            (HttpStatusCode)429 => true,                  // Too Many Requests
            HttpStatusCode.InternalServerError => true,   // 500
            HttpStatusCode.BadGateway => true,            // 502
            HttpStatusCode.ServiceUnavailable => true,    // 503
            HttpStatusCode.GatewayTimeout => true,        // 504
            _ => false,
        };

        private TimeSpan Backoff(int attempt)
        {
            var baseMs = _settings.BaseDelayMilliseconds <= 0 ? 200 : _settings.BaseDelayMilliseconds;
            var maxMs = _settings.MaxDelayMilliseconds <= 0 ? 2000 : _settings.MaxDelayMilliseconds;
            // Exponential: base * 2^(attempt-1), capped, plus up to 50% jitter.
            var exp = baseMs * Math.Pow(2, attempt - 1);
            var capped = Math.Min(exp, maxMs);
            var jitter = _jitter.NextDouble() * 0.5 * capped;
            return TimeSpan.FromMilliseconds(capped + jitter);
        }
    }
}
