using System;
using System.Collections.Generic;

namespace DerasaX.Application.Services.Abstractions.Operations
{
    /// <summary>
    /// Phase 19 — shared, process-local health/heartbeat state for background jobs
    /// (e.g. the file-retention service). The job writes its last-run outcome here;
    /// the API's "background-jobs" health check and the operational-status surface
    /// read it. No secrets or tenant data are recorded — only job names + outcomes.
    /// </summary>
    public interface IBackgroundJobHealth
    {
        /// <summary>Record a completed run (success or failure) of a named job.</summary>
        void RecordRun(string job, bool success, string? note, int affected);

        /// <summary>Record that a named job is present but disabled by configuration.</summary>
        void RecordDisabled(string job, string? note = null);

        /// <summary>Immutable snapshot of every known job's last status.</summary>
        IReadOnlyDictionary<string, BackgroundJobStatus> Snapshot();
    }

    /// <summary>Last-known status of one background job (no sensitive data).</summary>
    public sealed class BackgroundJobStatus
    {
        public string Job { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public DateTime? LastRunUtc { get; set; }
        public bool LastSuccess { get; set; } = true;
        public string? LastNote { get; set; }
        public long RunsCompleted { get; set; }
        public int LastAffected { get; set; }
    }
}
