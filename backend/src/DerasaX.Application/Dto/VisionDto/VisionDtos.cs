using System;
using System.Collections.Generic;
using DerasaX.Application.Common;
using DerasaX.Domain.Enums;

namespace DerasaX.Application.Dto.VisionDto
{
    // -------- session lifecycle --------
    public class StartVisionSessionDto
    {
        public string? SchoolClassId { get; set; }
        public string? SubjectId { get; set; }
        public string? LessonId { get; set; }
        public string? Title { get; set; }
        public DateTime? SessionDate { get; set; }
        /// <summary>Optional per-session recognition threshold (0..1); defaults to 0.5.</summary>
        public decimal? RecognitionThreshold { get; set; }
        public string? Notes { get; set; }
    }

    public class VisionSessionDto
    {
        public string Id { get; set; } = string.Empty;
        public string TeacherId { get; set; } = string.Empty;
        public string? SchoolClassId { get; set; }
        public string? SubjectId { get; set; }
        public string? LessonId { get; set; }
        public string? Title { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public DateTime SessionDate { get; set; }
        public int FrameCount { get; set; }
        public decimal RecognitionThreshold { get; set; }
        public string? EngineKind { get; set; }
        public bool Degraded { get; set; }
        public string? ModelVersion { get; set; }
        public string? Notes { get; set; }
        public int PendingCandidates { get; set; }
        public int ConfirmedAttendance { get; set; }
        public int RejectedCandidates { get; set; }
    }

    public class VisionSessionParameters : PaginationParameters
    {
        public string? SchoolClassId { get; set; }
        public VisionSessionStatus? Status { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    // -------- frame analysis --------
    public class AnalyzeFrameDto
    {
        /// <summary>Ephemeral frame, base64 (optionally a data URL). Never persisted.</summary>
        public string ImageBase64 { get; set; } = string.Empty;
        public int? FrameIndex { get; set; }
        public string? CaptureLabel { get; set; }
        public bool? WantEngagement { get; set; }
    }

    public class VisionFaceResultDto
    {
        public string TrackId { get; set; } = string.Empty;
        public List<int> Bbox { get; set; } = new();
        public string? ExternalLabelId { get; set; }
        public decimal RecognitionConfidence { get; set; }
        public string RecognitionStatus { get; set; } = "unknown";
        public string Emotion { get; set; } = "Unknown";
        public decimal EmotionConfidence { get; set; }
        public string Engagement { get; set; } = "NotReady";
        public decimal EngagementConfidence { get; set; }
        public int EngagementFrames { get; set; }
        public int EngagementFramesRequired { get; set; }
        public string? MappedStudentId { get; set; }
        public string? MappedStudentName { get; set; }
        public string CandidateId { get; set; } = string.Empty;
        public List<string> QualityFlags { get; set; } = new();
    }

    public class FrameAnalysisResultDto
    {
        public string FrameAnalysisId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public int FrameIndex { get; set; }
        public int FacesDetected { get; set; }
        public string Engine { get; set; } = string.Empty;
        public bool Degraded { get; set; }
        public string? ModelVersion { get; set; }
        public List<VisionFaceResultDto> Results { get; set; } = new();
    }

    public class FrameAnalysisDto
    {
        public string Id { get; set; } = string.Empty;
        public int FrameIndex { get; set; }
        public string? CaptureLabel { get; set; }
        public int FacesDetected { get; set; }
        public string EngineKind { get; set; } = string.Empty;
        public bool Degraded { get; set; }
        public string? ModelVersion { get; set; }
        public DateTime AnalyzedAt { get; set; }
    }

    // -------- attendance candidates / review --------
    public class AttendanceCandidateDto
    {
        public string Id { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string TrackId { get; set; } = string.Empty;
        public string? ExternalLabelId { get; set; }
        public string? MappedStudentId { get; set; }
        public string? MappedStudentName { get; set; }
        public string? ResolvedStudentId { get; set; }
        public string? ResolvedStudentName { get; set; }
        public decimal BestRecognitionConfidence { get; set; }
        public string RecognitionStatus { get; set; } = "unknown";
        public int DetectionCount { get; set; }
        public DateTime FirstSeenAt { get; set; }
        public DateTime LastSeenAt { get; set; }
        public bool Degraded { get; set; }
        public string ReviewStatus { get; set; } = string.Empty;
        public string? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
        public string? ResolvedStatus { get; set; }
        public string? AttendanceRecordId { get; set; }
    }

    public class VisionCandidateParameters : PaginationParameters
    {
        public CandidateReviewStatus? ReviewStatus { get; set; }
    }

    public class ConfirmCandidateDto
    {
        /// <summary>Student to confirm. Optional ONLY when the candidate already has a
        /// mapped student (enrollment). Low-confidence/unknown candidates require it.</summary>
        public string? StudentId { get; set; }
        /// <summary>Attendance status to set (Present|Absent|Late|Excused). Defaults to Present.</summary>
        public string? Status { get; set; }
        public string? Notes { get; set; }
        /// <summary>If true, remember externalLabel -> student as an enrollment mapping.</summary>
        public bool Remember { get; set; }
    }

    public class RejectCandidateDto
    {
        public string? Notes { get; set; }
    }

    public class OverrideCandidateDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string? Notes { get; set; }
    }

    // -------- summaries / analytics --------
    public class SessionSummaryDto
    {
        public string SessionId { get; set; } = string.Empty;
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

    /// <summary>Safe read-only engagement/attendance summary for a single student
    /// (student's own data, or a parent's linked child). No other students, no
    /// biometric artifacts.</summary>
    public class StudentEngagementSummaryDto
    {
        public string StudentId { get; set; } = string.Empty;
        public int EngagedObservations { get; set; }
        public int DisengagedObservations { get; set; }
        public int NotReadyObservations { get; set; }
        public decimal AverageEngagementConfidence { get; set; }
        public int SessionsObserved { get; set; }
        public int CvAttendanceCount { get; set; }
        public DateTime? LastObservedAt { get; set; }
        public bool Degraded { get; set; }
    }

    // -------- enrollment (identity mapping) --------
    public class EnrollFaceDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string ExternalLabelId { get; set; } = string.Empty;
        public string? DisplayLabel { get; set; }
    }

    public class FaceEnrollmentDto
    {
        public string Id { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string? StudentName { get; set; }
        public string ExternalLabelId { get; set; } = string.Empty;
        public string? DisplayLabel { get; set; }
        public bool IsActive { get; set; }
        public string Source { get; set; } = string.Empty;
        public DateTime EnrolledAt { get; set; }
    }
}
