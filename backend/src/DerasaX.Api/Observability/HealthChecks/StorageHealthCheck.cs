using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DerasaX.Api.Observability.HealthChecks
{
    /// <summary>
    /// Phase 19 — durable-storage provider health. Non-gating (not tagged "ready"): a
    /// storage outage must not break login/readiness; file operations surface their own
    /// honest 503 (StorageUnavailableException). Reports the active provider and whether
    /// it is usable, NEVER any secret/credential.
    /// </summary>
    public sealed class StorageHealthCheck : IHealthCheck
    {
        private readonly FileStorageSettings _settings;

        public StorageHealthCheck(IOptions<FileStorageSettings> settings) => _settings = settings.Value;

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var isS3 = _settings.Provider.Equals("S3", StringComparison.OrdinalIgnoreCase);
            var data = new Dictionary<string, object> { ["provider"] = isS3 ? "S3" : "Local" };

            if (isS3)
            {
                // No live PUT/GET here (would need real creds/network); report configured-ness honestly.
                return Task.FromResult(_settings.S3.IsConfigured
                    ? HealthCheckResult.Healthy("S3 provider configured.", data)
                    : HealthCheckResult.Degraded("S3 provider selected but not configured; uploads will return 503.", data: data));
            }

            try
            {
                var root = _settings.Local.RootPath;
                if (!Path.IsPathRooted(root))
                    root = Path.Combine(AppContext.BaseDirectory, root);
                if (!Directory.Exists(root))
                    Directory.CreateDirectory(root);
                data["rootExists"] = Directory.Exists(root);
                return Task.FromResult(HealthCheckResult.Healthy("Local storage root is accessible.", data));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Local storage root is not accessible.", ex, data));
            }
        }
    }
}
