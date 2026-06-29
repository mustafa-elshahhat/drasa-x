using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DerasaX.Api.Observability
{
    /// <summary>
    /// Phase 19 — machine-readable aggregate health document for /health. Anonymous-safe:
    /// emits only check names + statuses + safe descriptions + the service version/uptime.
    /// No secrets, connection strings, tenant data, or stack traces.
    /// </summary>
    public static class HealthCheckResponseWriter
    {
        public static Task WriteAsync(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";

            var payload = new
            {
                status = report.Status.ToString(),
                service = DeploymentInfo.ServiceName,
                version = DeploymentInfo.Version,
                environment = context.RequestServices
                    .GetService(typeof(Microsoft.AspNetCore.Hosting.IWebHostEnvironment)) is Microsoft.AspNetCore.Hosting.IWebHostEnvironment env
                    ? env.EnvironmentName : "unknown",
                totalDurationMs = (long)report.TotalDuration.TotalMilliseconds,
                checks = MapChecks(report)
            };

            var json = JsonSerializer.Serialize(payload);
            return context.Response.WriteAsync(json);
        }

        private static object[] MapChecks(HealthReport report)
        {
            var list = new System.Collections.Generic.List<object>();
            foreach (var entry in report.Entries)
            {
                list.Add(new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    durationMs = (long)entry.Value.Duration.TotalMilliseconds
                });
            }
            return list.ToArray();
        }
    }
}
