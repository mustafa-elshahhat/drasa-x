using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DerasaX.Api.Observability.HealthChecks
{
    /// <summary>
    /// Phase 19 — AI dependency (school-ai-rag) reachability. Non-gating (not tagged
    /// "ready"): the AI service being down must not make the backend report "not ready"
    /// for non-AI traffic; AI calls fast-fail via the circuit breaker (502). Probes the
    /// AI service's anonymous /health/live with a short timeout. No token, no payload.
    /// </summary>
    public sealed class AiServiceHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl;

        public AiServiceHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _baseUrl = (configuration["AiService:BaseUrl"] ?? "http://localhost:8000").TrimEnd('/');
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object> { ["baseUrl"] = _baseUrl };
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            try
            {
                var client = _httpClientFactory.CreateClient();
                using var resp = await client.GetAsync($"{_baseUrl}/health/live", cts.Token);
                data["status"] = (int)resp.StatusCode;
                return resp.IsSuccessStatusCode
                    ? HealthCheckResult.Healthy("AI service reachable.", data)
                    : HealthCheckResult.Degraded($"AI service returned {(int)resp.StatusCode}.", data: data);
            }
            catch (Exception ex)
            {
                // Degraded (not Unhealthy): AI being down is tolerated by the backend.
                return HealthCheckResult.Degraded("AI service unreachable.", ex, data);
            }
        }
    }
}
