using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DerasaX.Api.Observability
{
    /// <summary>
    /// Phase 19 — request correlation + structured request logging + metrics.
    /// Resolves a correlation id (inbound X-Correlation-Id, sanitized, or a new GUID),
    /// stores it in HttpContext (so error responses echo the same id), pushes it into a
    /// logging scope so EVERY log line in the request carries {CorrelationId}, echoes it on
    /// the response, and records request metrics. The logged path is masked
    /// (<see cref="LogSanitizer"/>) so signed-download tokens never reach the logs.
    /// </summary>
    public sealed class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-Id";
        public const string ItemKey = "CorrelationId";
        private const int MaxLen = 128;

        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;
        private readonly IRuntimeMetrics _metrics;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger, IRuntimeMetrics metrics)
        {
            _next = next;
            _logger = logger;
            _metrics = metrics;
        }

        public async Task Invoke(HttpContext context)
        {
            var correlationId = ResolveCorrelationId(context);
            context.Items[ItemKey] = correlationId;
            context.TraceIdentifier = correlationId;

            // Echo on the response even if a downstream handler clears the response (errors).
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            var sw = Stopwatch.StartNew();
            using (_logger.BeginScope(new Dictionary<string, object> { [ItemKey] = correlationId }))
            {
                try
                {
                    await _next(context);
                }
                finally
                {
                    sw.Stop();
                    var status = context.Response?.StatusCode ?? 0;
                    _metrics.RecordRequest(status, sw.Elapsed.TotalMilliseconds);
                    _logger.LogInformation(
                        "request {Method} {Path} -> {StatusCode} in {ElapsedMs}ms",
                        context.Request.Method,
                        LogSanitizer.MaskQuery(context.Request.Path, context.Request.QueryString),
                        status,
                        (long)sw.Elapsed.TotalMilliseconds);
                }
            }
        }

        private static string ResolveCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(HeaderName, out var inbound))
            {
                var candidate = inbound.ToString();
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    candidate = candidate.Trim();
                    if (candidate.Length > MaxLen) candidate = candidate.Substring(0, MaxLen);
                    // Keep only safe characters to avoid log/header injection.
                    if (candidate.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.'))
                        return candidate;
                }
            }
            return Guid.NewGuid().ToString("N");
        }
    }
}
