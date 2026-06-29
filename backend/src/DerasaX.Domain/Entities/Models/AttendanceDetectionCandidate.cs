using System;
using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// A CV-detected attendance CANDIDATE (Phase 15). One row per (session, track) — a
    /// single tracked face/person in a session — upserted as frames arrive. A candidate
    /// is NEVER attendance by itself: an authorized teacher/admin must confirm, reject,
    /// or override it. Low-confidence and unknown detections cannot be confirmed without
    /// the reviewer explicitly choosing a student. Cross-tenant mapping is impossible
    /// (tenant-filtered + same-tenant student validation on confirm).
    /// </summary>
    public class AttendanceDetectionCandidate : AuditableEntity<string>
    {
        public string SessionId { get; set; } = string.Empty;
        public ClassroomVisionSession? Session { get; set; }

        /// <summary>Temporary, session-scoped track id from the AI service.</summary>
        public string TrackId { get; set; } = string.Empty;

        /// <summary>Opaque external label/cluster id — NOT a DerasaX student id.</summary>
        public string? ExternalLabelId { get; set; }

        /// <summary>Pre-filled student id when an enrollment maps the external label
        /// (still requires review before attendance is created).</summary>
        public string? MappedStudentId { get; set; }

        /// <summary>Best recognition confidence seen across frames (0..1).</summary>
        public decimal BestRecognitionConfidence { get; set; }

        /// <summary>"candidate" | "low_confidence" | "unknown" (as returned by the AI).</summary>
        public string RecognitionStatus { get; set; } = "unknown";

        public int DetectionCount { get; set; }
        public DateTime FirstSeenAt { get; set; }
        public DateTime LastSeenAt { get; set; }

        public bool Degraded { get; set; }

        // ---- review ----
        public CandidateReviewStatus ReviewStatus { get; set; } = CandidateReviewStatus.Pending;
        public string? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }

        /// <summary>The student the reviewer confirmed/overrode this candidate to.</summary>
        public string? ResolvedStudentId { get; set; }

        /// <summary>The attendance status set on confirm/override (Present by default).</summary>
        public AttendanceStatus? ResolvedStatus { get; set; }

        /// <summary>Link to the StudentAttendanceRecord created on confirm/override.</summary>
        public string? AttendanceRecordId { get; set; }
    }
}
