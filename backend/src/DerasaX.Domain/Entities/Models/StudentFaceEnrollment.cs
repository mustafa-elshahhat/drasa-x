using System;
using DerasaX.Domain.Entities.Base;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// Tenant-scoped, role-authorized, AUDITABLE mapping from an opaque external face
    /// label/cluster id to a DerasaX student (Phase 15). This is the ONLY approved way a
    /// CV detection becomes associated with a student identity — identity remains
    /// backend-owned. No raw biometric (image/embedding) is stored here; only the opaque
    /// external label id and the student id. Enrollment is optional: without it, CV
    /// recognition stays Unknown / review-required (documented limitation, not faked).
    /// </summary>
    public class StudentFaceEnrollment : AuditableEntity<string>
    {
        public string StudentId { get; set; } = string.Empty;
        public Student? Student { get; set; }

        /// <summary>Opaque external label/cluster id produced by the AI service.</summary>
        public string ExternalLabelId { get; set; } = string.Empty;

        /// <summary>Optional human-readable label for staff (e.g. "front row, glasses").</summary>
        public string? DisplayLabel { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>How the mapping was created (e.g. "manual", "confirm-remember").</summary>
        public string Source { get; set; } = "manual";

        public DateTime EnrolledAt { get; set; }

        // --- Phase 16: optional consented durable enrollment asset (default OFF, never raw frames) ---
        /// <summary>True only when explicit consent was captured to store an enrollment reference image.</summary>
        public bool ConsentObtained { get; set; }
        /// <summary>Reference to the consent basis/record (no PII).</summary>
        public string? ConsentReference { get; set; }
        /// <summary>Retention deadline for the consented asset; eligible for purge afterward.</summary>
        public DateTime? AssetRetentionUntil { get; set; }
        /// <summary>Durable file backing the consented enrollment image (null = none stored).</summary>
        public string? FileRecordId { get; set; }
    }
}
