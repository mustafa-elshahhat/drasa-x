namespace DerasaX.Application.Common
{
    /// <summary>
    /// Configuration for the internal AI service (school-ai-rag) that the backend
    /// calls over authenticated HTTP. Bound from the <c>AiService</c> section. No
    /// production URL is hardcoded; the base URL comes from configuration/env.
    /// </summary>
    public class AiServiceSettings
    {
        public const string SectionName = "AiService";

        /// <summary>Base URL of school-ai-rag, e.g. http://localhost:8000.</summary>
        public string BaseUrl { get; set; } = "http://localhost:8000";

        /// <summary>Overall per-request timeout (connect+read) in seconds.</summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>Resilience policy (retry + circuit breaker) for the AI client (§15).</summary>
        public AiResilienceSettings Resilience { get; set; } = new();
    }

    /// <summary>
    /// Bounded transient-retry + circuit-breaker configuration for the internal
    /// AI client (Phase 6 §15). Retries apply ONLY to transient failures
    /// (timeout, transport, 408/429/5xx) and never to deterministic 4xx
    /// (validation/auth/scope) responses.
    /// </summary>
    public class AiResilienceSettings
    {
        /// <summary>Maximum transient retries (total attempts = MaxRetries + 1).</summary>
        public int MaxRetries { get; set; } = 2;

        /// <summary>Base backoff in milliseconds (exponential per attempt).</summary>
        public int BaseDelayMilliseconds { get; set; } = 200;

        /// <summary>Upper bound for a single backoff delay in milliseconds.</summary>
        public int MaxDelayMilliseconds { get; set; } = 2000;

        /// <summary>Consecutive provider failures before the circuit opens.</summary>
        public int CircuitFailureThreshold { get; set; } = 5;

        /// <summary>Seconds the circuit stays open before a half-open trial.</summary>
        public int CircuitResetSeconds { get; set; } = 30;
    }
}
