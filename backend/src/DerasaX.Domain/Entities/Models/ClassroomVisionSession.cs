using System;
using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// A teacher/school-admin-initiated computer-vision session for one class/lesson
    /// (Phase 15). The system of record is PostgreSQL, owned by DerasaX-backend. The
    /// session persists only NORMALIZED analysis metadata — never the raw camera feed,
    /// face crops, or embeddings (privacy minimization; <see cref="StoreRawFrames"/>
    /// stays false in Phase 15).
    /// </summary>
    public class ClassroomVisionSession : AuditableEntity<string>
    {
        /// <summary>The staff member who started the session (Teacher or SchoolAdmin).</summary>
        public string TeacherId { get; set; } = string.Empty;

        public string? SchoolClassId { get; set; }
        public SchoolClass? SchoolClass { get; set; }

        public string? SubjectId { get; set; }
        public string? LessonId { get; set; }

        public string? Title { get; set; }
        public VisionSessionStatus Status { get; set; } = VisionSessionStatus.Active;

        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }

        /// <summary>Date the resulting attendance applies to (defaults to StartedAt's date).</summary>
        public DateTime SessionDate { get; set; }

        public int FrameCount { get; set; }

        /// <summary>Configurable recognition threshold used for this session (0..1).</summary>
        public decimal RecognitionThreshold { get; set; } = 0.5m;

        /// <summary>Engine that produced this session's analyses (recorded from first frame).</summary>
        public VisionEngineKind? EngineKind { get; set; }

        /// <summary>True when a degraded/stub engine produced the data — surfaced honestly
        /// in the UI; degraded results are review-required and never auto-confirmed.</summary>
        public bool Degraded { get; set; }

        public string? ModelVersion { get; set; }

        /// <summary>Privacy minimization flag. Phase 15 never stores raw frames; this is
        /// false and documents the decision for auditors.</summary>
        public bool StoreRawFrames { get; set; } = false;

        public string? Notes { get; set; }
    }
}
