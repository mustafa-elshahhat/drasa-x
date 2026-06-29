using System;
using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// A per-face engagement + emotion analytics signal from one analyzed frame
    /// (Phase 15). Stored as an ANALYTICS observation, never as raw biometric data.
    /// <see cref="StudentId"/> is set only when the face is mapped to a tenant-scoped
    /// student via an approved enrollment; otherwise the observation is anonymous
    /// (track/external-label only) and aggregated at session/class level.
    /// </summary>
    public class StudentEngagementObservation : AuditableEntity<string>
    {
        public string SessionId { get; set; } = string.Empty;
        public ClassroomVisionSession? Session { get; set; }

        public string? FrameAnalysisId { get; set; }

        /// <summary>Temporary, session-scoped track id from the AI service.</summary>
        public string TrackId { get; set; } = string.Empty;

        /// <summary>Opaque external label/cluster id — NOT a DerasaX student id.</summary>
        public string? ExternalLabelId { get; set; }

        /// <summary>Mapped DerasaX student (only when an enrollment mapping exists).</summary>
        public string? StudentId { get; set; }

        public string Emotion { get; set; } = "Unknown";
        public decimal EmotionConfidence { get; set; }

        public EngagementLabel Engagement { get; set; } = EngagementLabel.NotReady;
        public decimal EngagementConfidence { get; set; }
        public int EngagementFrames { get; set; }
        public bool EngagementReady { get; set; }

        public VisionEngineKind EngineKind { get; set; }
        public bool Degraded { get; set; }

        public DateTime ObservedAt { get; set; }
    }
}
