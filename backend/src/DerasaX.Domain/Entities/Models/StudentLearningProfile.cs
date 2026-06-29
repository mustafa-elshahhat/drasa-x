using DerasaX.Domain.Entities.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// Phase 6 §12 — staff-recorded demographic / learning-context attributes for a
    /// student that the performance model requires but the academic schema does not
    /// otherwise store (a proven persistence gap). These are NOT authoritative
    /// academic metrics (attendance/study-time come from <see cref="StudentMetricHistory"/>,
    /// gender from the identity record); they are survey-style context maintained
    /// through an approved backend endpoint, never asserted by the browser at
    /// prediction time. One row per student.
    /// </summary>
    public class StudentLearningProfile : BaseEntity<string>
    {
        [ForeignKey("Student")]
        public string StudentId { get; set; } = string.Empty;
        public Student Student { get; set; } = null!;

        /// <summary>Student age in years (no DOB is stored in the schema).</summary>
        public int AgeYears { get; set; }

        /// <summary>"public" | "private".</summary>
        public string SchoolType { get; set; } = string.Empty;

        /// <summary>"yes" | "no".</summary>
        public string InternetAccess { get; set; } = string.Empty;

        /// <summary>"&lt;15 min" | "15-30 min" | "30-60 min" | "&gt;60 min".</summary>
        public string TravelTime { get; set; } = string.Empty;

        /// <summary>"yes" | "no".</summary>
        public string ExtraActivities { get; set; } = string.Empty;

        /// <summary>"textbook" | "notes" | "online videos" | "group study" | "mixed".</summary>
        public string StudyMethod { get; set; } = string.Empty;

        /// <summary>Feature-schema version this profile is shaped for.</summary>
        public string FeatureSchemaVersion { get; set; } = "perf-v1";
    }
}
