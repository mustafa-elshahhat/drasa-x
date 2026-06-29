using System;
using System.Threading;

namespace DerasaX.Api.Observability
{
    /// <summary>
    /// Phase 19 — process-local request metrics (counts + average latency + uptime).
    /// A lightweight, dependency-free "metrics-ready" abstraction fed by
    /// <see cref="CorrelationIdMiddleware"/> and surfaced (admin-only) via the
    /// operational-status endpoint. It is NOT a public metrics endpoint and never
    /// records request bodies, paths, secrets, tenants, or users.
    /// </summary>
    public interface IRuntimeMetrics
    {
        void RecordRequest(int statusCode, double elapsedMs);
        RuntimeMetricsSnapshot Snapshot();
    }

    public sealed class RuntimeMetricsSnapshot
    {
        public long TotalRequests { get; set; }
        public long Status2xx { get; set; }
        public long Status3xx { get; set; }
        public long Status4xx { get; set; }
        public long Status5xx { get; set; }
        public long ServerErrors { get; set; }
        public double AvgLatencyMs { get; set; }
        public double UptimeSeconds { get; set; }
        public DateTime ProcessStartUtc { get; set; }
    }

    public sealed class RuntimeMetrics : IRuntimeMetrics
    {
        private readonly DateTime _startUtc = DateTime.UtcNow;
        private long _total, _s2, _s3, _s4, _s5, _latencyMicros;

        public void RecordRequest(int statusCode, double elapsedMs)
        {
            Interlocked.Increment(ref _total);
            if (statusCode >= 200 && statusCode < 300) Interlocked.Increment(ref _s2);
            else if (statusCode >= 300 && statusCode < 400) Interlocked.Increment(ref _s3);
            else if (statusCode >= 400 && statusCode < 500) Interlocked.Increment(ref _s4);
            else if (statusCode >= 500) Interlocked.Increment(ref _s5);
            // Accumulate latency in microseconds to keep an integer running sum.
            Interlocked.Add(ref _latencyMicros, (long)(elapsedMs * 1000.0));
        }

        public RuntimeMetricsSnapshot Snapshot()
        {
            var total = Interlocked.Read(ref _total);
            var micros = Interlocked.Read(ref _latencyMicros);
            return new RuntimeMetricsSnapshot
            {
                TotalRequests = total,
                Status2xx = Interlocked.Read(ref _s2),
                Status3xx = Interlocked.Read(ref _s3),
                Status4xx = Interlocked.Read(ref _s4),
                Status5xx = Interlocked.Read(ref _s5),
                ServerErrors = Interlocked.Read(ref _s5),
                AvgLatencyMs = total > 0 ? Math.Round((micros / 1000.0) / total, 2) : 0,
                UptimeSeconds = Math.Round((DateTime.UtcNow - _startUtc).TotalSeconds, 1),
                ProcessStartUtc = _startUtc
            };
        }
    }
}
