using DerasaX.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DerasaX.Domain.Entities.Models
{
    public class ApplicationUser: IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string LoginCode { get; set; } = null!;
        // Nullable so a platform-scoped SystemAdmin can exist without a tenant
        // (Phase 2 AUTHENTICATION_FLOW §6). Tenant-role users always have one.
        [ForeignKey("Tenant")]
        public string? TenantId { get; set; }
        public Tenant? Tenant { get; set; }
        /// <summary>When true the account is disabled and may not authenticate.</summary>
        public bool IsDeleted { get; set; } = false;
        /// <summary>When true the account must change its password before using any other endpoint
        /// (set on provisioning and on credential reset; cleared on a successful password change).</summary>
        public bool MustChangePassword { get; set; } = false;
        public Gender? Gender { get; set; }
        public List<RefreshToken>? refreshTokens { get; set; }
        public ICollection<Post> posts { get; set; } = new HashSet<Post>();
        public ICollection<Notification> notifications { get; set; } = new HashSet<Notification>();
        public ICollection<SupportRequest> supportRequests { get; set; } = new HashSet<SupportRequest>();
    }
    public class Student:ApplicationUser
    {
        [ForeignKey("Grade")]
        public string GradeId { get; set; } = null!;
        public Grade Grade { get; set; } = null!;
        public ICollection<StudentInsight> studentInsights { get; set; } = new HashSet<StudentInsight>();
        public ICollection<StudentLessonProgress> studentLessonProgresses { get; set; } = new HashSet<StudentLessonProgress>();
        public ICollection<QuizSubmission> quizSubmissions { get; set; } = new HashSet<QuizSubmission>();
        public ICollection<SubjectProgress> subjectProgresses { get; set; } = new HashSet<SubjectProgress>();
        public ICollection<StudentMetricHistory> metricHistory { get; set; } = new HashSet<StudentMetricHistory>();
        public ICollection<PainPoint> painPoints { get; set; } = new HashSet<PainPoint>();
        public ICollection<StudentRecommendation> recommendations { get; set; } = new HashSet<StudentRecommendation>();
        public ICollection<PredictionRecord> predictionRecords { get; set; } = new HashSet<PredictionRecord>();

    }
    public class Teacher : ApplicationUser
    {
    }
    public class Parent : ApplicationUser
    {
    }
    public class SchoolAdmin : ApplicationUser
    {
    }
    public class SystemAdmin:ApplicationUser
    {
    }

}
