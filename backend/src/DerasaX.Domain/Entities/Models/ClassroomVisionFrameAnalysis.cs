using System;
using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// Frame-level metadata for one analyzed ephemeral frame (Phase 15). The frame
    /// image itself is NOT persisted — only the normalized counts/metadata returned by
    /// the AI service. Per-face signals live in <see cref="StudentEngagementObservation"/>
    /// and <see cref="AttendanceDetectionCandidate"/>.
    /// </summary>
    public class ClassroomVisionFrameAnalysis : AuditableEntity<string>
    {
        public string SessionId { get; set; } = string.Empty;
        public ClassroomVisionSession? Session { get; set; }

        public int FrameIndex { get; set; }
        public string? CaptureLabel { get; set; }

        public int FacesDetected { get; set; }

        public VisionEngineKind EngineKind { get; set; }
        public bool Degraded { get; set; }
        public string? ModelVersion { get; set; }
        public string? CorrelationId { get; set; }

        /// <summary>Comma-separated frame-level quality flags (e.g. "degraded_stub_engine").</summary>
        public string? QualityFlags { get; set; }

        public DateTime AnalyzedAt { get; set; }
    }
}
