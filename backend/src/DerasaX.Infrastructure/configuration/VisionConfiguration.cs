using DerasaX.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DerasaX.Infrastructure.configuration
{
    // Phase 15 — Computer-vision attendance + engagement. Tenant integrity is enforced
    // by the global filter/FK in DerasaXDbContext; these configs add lengths, precision,
    // same-tenant composite FKs, and the unique indexes that make upserts idempotent.

    public class ClassroomVisionSessionConfiguration : IEntityTypeConfiguration<ClassroomVisionSession>
    {
        public void Configure(EntityTypeBuilder<ClassroomVisionSession> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.TeacherId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.SchoolClassId).HasMaxLength(450);
            builder.Property(x => x.SubjectId).HasMaxLength(450);
            builder.Property(x => x.LessonId).HasMaxLength(450);
            builder.Property(x => x.Title).HasMaxLength(256);
            builder.Property(x => x.ModelVersion).HasMaxLength(64);
            builder.Property(x => x.Notes).HasMaxLength(2048);
            builder.Property(x => x.RecognitionThreshold).HasPrecision(5, 4);
            // Nullable enums are not picked up by ApplyEnumToStringConversions; map
            // them to strings explicitly to stay consistent with non-nullable enums.
            builder.Property(x => x.EngineKind).HasConversion<string>().HasMaxLength(16);

            // Same-tenant integrity for FK children referencing this session.
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });

            builder.HasOne(x => x.SchoolClass)
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SchoolClassId })
                .HasPrincipalKey(c => new { c.TenantId, c.Id })
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasIndex(x => new { x.TenantId, x.Status });
            builder.HasIndex(x => new { x.TenantId, x.SchoolClassId, x.SessionDate });
            builder.HasIndex(x => new { x.TenantId, x.TeacherId, x.SessionDate });
        }
    }

    public class ClassroomVisionFrameAnalysisConfiguration : IEntityTypeConfiguration<ClassroomVisionFrameAnalysis>
    {
        public void Configure(EntityTypeBuilder<ClassroomVisionFrameAnalysis> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.SessionId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.CaptureLabel).HasMaxLength(64);
            builder.Property(x => x.ModelVersion).HasMaxLength(64);
            builder.Property(x => x.CorrelationId).HasMaxLength(128);
            builder.Property(x => x.QualityFlags).HasMaxLength(512);

            builder.HasOne(x => x.Session)
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SessionId })
                .HasPrincipalKey(s => new { s.TenantId, s.Id })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.SessionId, x.FrameIndex });
        }
    }

    public class StudentEngagementObservationConfiguration : IEntityTypeConfiguration<StudentEngagementObservation>
    {
        public void Configure(EntityTypeBuilder<StudentEngagementObservation> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.SessionId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.FrameAnalysisId).HasMaxLength(450);
            builder.Property(x => x.TrackId).IsRequired().HasMaxLength(128);
            builder.Property(x => x.ExternalLabelId).HasMaxLength(128);
            builder.Property(x => x.StudentId).HasMaxLength(450);
            builder.Property(x => x.Emotion).IsRequired().HasMaxLength(32);
            builder.Property(x => x.EmotionConfidence).HasPrecision(5, 4);
            builder.Property(x => x.EngagementConfidence).HasPrecision(5, 4);

            builder.HasOne(x => x.Session)
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SessionId })
                .HasPrincipalKey(s => new { s.TenantId, s.Id })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.SessionId });
            builder.HasIndex(x => new { x.TenantId, x.StudentId, x.ObservedAt });
        }
    }

    public class AttendanceDetectionCandidateConfiguration : IEntityTypeConfiguration<AttendanceDetectionCandidate>
    {
        public void Configure(EntityTypeBuilder<AttendanceDetectionCandidate> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.SessionId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.TrackId).IsRequired().HasMaxLength(128);
            builder.Property(x => x.ExternalLabelId).HasMaxLength(128);
            builder.Property(x => x.MappedStudentId).HasMaxLength(450);
            builder.Property(x => x.ResolvedStudentId).HasMaxLength(450);
            builder.Property(x => x.AttendanceRecordId).HasMaxLength(450);
            builder.Property(x => x.ReviewedByUserId).HasMaxLength(450);
            builder.Property(x => x.RecognitionStatus).IsRequired().HasMaxLength(32);
            builder.Property(x => x.ReviewNotes).HasMaxLength(1024);
            builder.Property(x => x.BestRecognitionConfidence).HasPrecision(5, 4);
            builder.Property(x => x.ResolvedStatus).HasConversion<string>().HasMaxLength(16);

            builder.HasOne(x => x.Session)
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SessionId })
                .HasPrincipalKey(s => new { s.TenantId, s.Id })
                .OnDelete(DeleteBehavior.Restrict);

            // One candidate per tracked face per session (idempotent upsert key).
            builder.HasIndex(x => new { x.TenantId, x.SessionId, x.TrackId }).IsUnique();
            builder.HasIndex(x => new { x.TenantId, x.SessionId, x.ReviewStatus });
        }
    }

    public class ClassroomVisionSessionSummaryConfiguration : IEntityTypeConfiguration<ClassroomVisionSessionSummary>
    {
        public void Configure(EntityTypeBuilder<ClassroomVisionSessionSummary> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.SessionId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.AverageEngagementConfidence).HasPrecision(5, 4);

            builder.HasOne(x => x.Session)
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SessionId })
                .HasPrincipalKey(s => new { s.TenantId, s.Id })
                .OnDelete(DeleteBehavior.Restrict);

            // One summary per session.
            builder.HasIndex(x => new { x.TenantId, x.SessionId }).IsUnique();
        }
    }

    public class StudentFaceEnrollmentConfiguration : IEntityTypeConfiguration<StudentFaceEnrollment>
    {
        public void Configure(EntityTypeBuilder<StudentFaceEnrollment> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.ExternalLabelId).IsRequired().HasMaxLength(128);
            builder.Property(x => x.DisplayLabel).HasMaxLength(128);
            builder.Property(x => x.Source).IsRequired().HasMaxLength(32);
            // Phase 16 — consented durable enrollment asset metadata.
            builder.Property(x => x.ConsentReference).HasMaxLength(256);
            builder.Property(x => x.FileRecordId).HasMaxLength(450);

            builder.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // One mapping per external label per tenant (the recognition lookup key).
            builder.HasIndex(x => new { x.TenantId, x.ExternalLabelId }).IsUnique();
            builder.HasIndex(x => new { x.TenantId, x.StudentId });
        }
    }
}
