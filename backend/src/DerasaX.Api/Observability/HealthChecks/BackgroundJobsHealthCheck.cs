using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Services.Abstractions.Operations;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DerasaX.Api.Observability.HealthChecks
{
    /// <summary>
    /// Phase 19 — background-job (file-retention) heartbeat. Non-gating. Reports Healthy
    /// when jobs are disabled (a valid local posture) or last ran successfully; Degraded
    /// when a job's last run failed. Surfaces last-run time + affected counts (no data).
    /// </summary>
    public sealed class BackgroundJobsHealthCheck : IHealthCheck
    {
        private readonly IBackgroundJobHealth _health;

        public BackgroundJobsHealthCheck(IBackgroundJobHealth health) => _health = health;

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var snap = _health.Snapshot();
            var data = new Dictionary<string, object>();
            foreach (var kv in snap)
            {
                data[kv.Key] = new
                {
                    enabled = kv.Value.Enabled,
                    lastRunUtc = kv.Value.LastRunUtc,
                    lastSuccess = kv.Value.LastSuccess,
                    runs = kv.Value.RunsCompleted,
                    lastAffected = kv.Value.LastAffected,
                    note = kv.Value.LastNote
                };
            }

            if (snap.Count == 0)
                return Task.FromResult(HealthCheckResult.Healthy("No background jobs have reported yet.", data));

            var anyFailed = snap.Values.Any(s => !s.LastSuccess);
            return Task.FromResult(anyFailed
                ? HealthCheckResult.Degraded("A background job's last run failed.", data: data)
                : HealthCheckResult.Healthy("Background jobs healthy (or disabled).", data));
        }
    }
}
