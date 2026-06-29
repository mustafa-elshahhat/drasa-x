using System;
using DerasaX.Domain.Entities.Base;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// Aggregated, persisted summary for one CV session (Phase 15). Recomputed from the
    /// session's frame analyses, engagement observations, and attendance candidates when
    /// the summary is requested and when the session ends. Contains only aggregate counts
    /// — no per-face biometric data.
    /// </summary>
    public class ClassroomVisionSessionSummary : AuditableEntity<string>
    {
        public string SessionId { get; set; } = string.Empty;
        public ClassroomVisionSession? Session { get; set; }

        public int TotalFrames { get; set; }
        public int TotalFaceObservations { get; set; }
        public int DistinctTracks { get; set; }

        public int PendingCandidates { get; set; }
        public int ConfirmedAttendance { get; set; }
        public int RejectedCandidates { get; set; }
        public int OverriddenCandidates { get; set; }

        public int EngagedObservations { get; set; }
        public int DisengagedObservations { get; set; }
        public int NotReadyObservations { get; set; }
        public decimal AverageEngagementConfidence { get; set; }

        public bool Degraded { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
