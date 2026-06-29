using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DerasaX.Application.Services.Abstractions.Operations;

namespace DerasaX.Application.Services.Operations
{
    /// <summary>
    /// Phase 19 — thread-safe, in-memory implementation of <see cref="IBackgroundJobHealth"/>.
    /// Registered as a singleton so the hosted background service and the API health
    /// check/operational-status surface share the same state.
    /// </summary>
    public sealed class BackgroundJobHealth : IBackgroundJobHealth
    {
        private readonly ConcurrentDictionary<string, BackgroundJobStatus> _jobs = new();

        public void RecordRun(string job, bool success, string? note, int affected)
        {
            _jobs.AddOrUpdate(job,
                _ => new BackgroundJobStatus
                {
                    Job = job, Enabled = true, LastRunUtc = DateTime.UtcNow,
                    LastSuccess = success, LastNote = note, RunsCompleted = 1, LastAffected = affected
                },
                (_, existing) =>
                {
                    existing.Enabled = true;
                    existing.LastRunUtc = DateTime.UtcNow;
                    existing.LastSuccess = success;
                    existing.LastNote = note;
                    existing.RunsCompleted += 1;
                    existing.LastAffected = affected;
                    return existing;
                });
        }

        public void RecordDisabled(string job, string? note = null)
        {
            _jobs.AddOrUpdate(job,
                _ => new BackgroundJobStatus { Job = job, Enabled = false, LastSuccess = true, LastNote = note ?? "disabled" },
                (_, existing) => { existing.Enabled = false; existing.LastNote = note ?? "disabled"; return existing; });
        }

        public IReadOnlyDictionary<string, BackgroundJobStatus> Snapshot()
        {
            // Shallow copy so callers cannot mutate the live state.
            var copy = new Dictionary<string, BackgroundJobStatus>();
            foreach (var kv in _jobs)
            {
                copy[kv.Key] = new BackgroundJobStatus
                {
                    Job = kv.Value.Job, Enabled = kv.Value.Enabled, LastRunUtc = kv.Value.LastRunUtc,
                    LastSuccess = kv.Value.LastSuccess, LastNote = kv.Value.LastNote,
                    RunsCompleted = kv.Value.RunsCompleted, LastAffected = kv.Value.LastAffected
                };
            }
            return copy;
        }
    }
}
