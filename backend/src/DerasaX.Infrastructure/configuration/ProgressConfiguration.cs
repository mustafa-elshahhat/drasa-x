using DerasaX.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DerasaX.Infrastructure.configuration
{
    public class StudentLessonProgressConfiguration : IEntityTypeConfiguration<StudentLessonProgress>
    {
        public void Configure(EntityTypeBuilder<StudentLessonProgress> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.LessonId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.CompletionPercentage).HasPrecision(5, 2);

            builder.HasOne(x => x.Student)
                .WithMany(s => s.studentLessonProgresses)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Lesson)
                .WithMany(l => l.studentLessonProgresses)
                .HasForeignKey(x => new { x.TenantId, x.LessonId })
                .HasPrincipalKey(l => new { l.TenantId, l.Id })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.StudentId, x.LessonId }).IsUnique();
        }
    }

    public class StudentAttendanceRecordConfiguration : IEntityTypeConfiguration<StudentAttendanceRecord>
    {
        public void Configure(EntityTypeBuilder<StudentAttendanceRecord> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.SchoolClassId).HasMaxLength(450);
            builder.Property(x => x.SessionKey).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Notes).HasMaxLength(1024);

            builder.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SchoolClass)
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SchoolClassId })
                .HasPrincipalKey(c => new { c.TenantId, c.Id })
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasIndex(x => new { x.TenantId, x.StudentId, x.AttendanceDate, x.SessionKey }).IsUnique();
            builder.HasIndex(x => new { x.TenantId, x.StudentId, x.AttendanceDate });
            builder.HasIndex(x => new { x.TenantId, x.SchoolClassId, x.AttendanceDate });
        }
    }

    public class StudentLearningProfileConfiguration : IEntityTypeConfiguration<StudentLearningProfile>
    {
        public void Configure(EntityTypeBuilder<StudentLearningProfile> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.SchoolType).HasMaxLength(32);
            builder.Property(x => x.InternetAccess).HasMaxLength(16);
            builder.Property(x => x.TravelTime).HasMaxLength(32);
            builder.Property(x => x.ExtraActivities).HasMaxLength(16);
            builder.Property(x => x.StudyMethod).HasMaxLength(64);
            builder.Property(x => x.FeatureSchemaVersion).HasMaxLength(32);

            builder.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // One learning profile per student per tenant.
            builder.HasIndex(x => new { x.TenantId, x.StudentId }).IsUnique();
        }
    }

    public class StudentInsightConfiguration : IEntityTypeConfiguration<StudentInsight>
    {
        public void Configure(EntityTypeBuilder<StudentInsight> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.ConfidenceScore).HasPrecision(5, 4);
            builder.Property(x => x.Summary).HasMaxLength(1024);
            builder.Property(x => x.RecommendationText).HasMaxLength(2048);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });

            builder.HasOne(x => x.Student)
                .WithMany(s => s.studentInsights)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.StudentId, x.PeriodStart, x.PeriodEnd });
        }
    }

    public class SubjectProgressConfiguration : IEntityTypeConfiguration<SubjectProgress>
    {
        public void Configure(EntityTypeBuilder<SubjectProgress> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.SubjectId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.CompletionPercentage).HasPrecision(5, 2);
            builder.Property(x => x.AverageScore).HasPrecision(5, 2);

            builder.HasOne(x => x.Student)
                .WithMany(s => s.subjectProgresses)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Subject)
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SubjectId })
                .HasPrincipalKey(s => new { s.TenantId, s.Id })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.StudentId, x.SubjectId }).IsUnique();
        }
    }

    public class StudentMetricHistoryConfiguration : IEntityTypeConfiguration<StudentMetricHistory>
    {
        public void Configure(EntityTypeBuilder<StudentMetricHistory> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Value).HasPrecision(12, 4);
            builder.Property(x => x.SourceEntityType).HasMaxLength(128);
            builder.Property(x => x.SourceEntityId).HasMaxLength(450);

            builder.HasOne(x => x.Student)
                .WithMany(s => s.metricHistory)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.StudentId, x.MetricType, x.MeasuredAt });
        }
    }

    public class PainPointConfiguration : IEntityTypeConfiguration<PainPoint>
    {
        public void Configure(EntityTypeBuilder<PainPoint> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentInsightId).HasMaxLength(450);
            builder.Property(x => x.Title).IsRequired().HasMaxLength(256);
            builder.Property(x => x.Description).HasMaxLength(2048);
            builder.Property(x => x.ConfidenceScore).HasPrecision(5, 4);
            builder.Property(x => x.Recommendation).HasMaxLength(2048);
            builder.Property(x => x.ReviewedByTeacherId).HasMaxLength(450);
            builder.Property(x => x.AiProvider).HasMaxLength(64);
            builder.Property(x => x.ModelVersion).HasMaxLength(64);
            builder.Property(x => x.PromptVersion).HasMaxLength(32);
            builder.Property(x => x.CorrelationId).HasMaxLength(64);

            builder.HasOne(x => x.Student)
                .WithMany(s => s.painPoints)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.StudentInsight)
                .WithMany(i => i.PainPoints)
                .HasForeignKey(x => new { x.TenantId, x.StudentInsightId })
                .HasPrincipalKey(i => new { i.TenantId, i.Id })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.StudentId, x.IsResolved });
        }
    }

    public class StudentRecommendationConfiguration : IEntityTypeConfiguration<StudentRecommendation>
    {
        public void Configure(EntityTypeBuilder<StudentRecommendation> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentInsightId).HasMaxLength(450);
            builder.Property(x => x.Title).IsRequired().HasMaxLength(256);
            builder.Property(x => x.Body).IsRequired().HasMaxLength(2048);

            builder.HasOne(x => x.Student)
                .WithMany(s => s.recommendations)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.StudentInsight)
                .WithMany(i => i.Recommendations)
                .HasForeignKey(x => new { x.TenantId, x.StudentInsightId })
                .HasPrincipalKey(i => new { i.TenantId, i.Id })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.StudentId, x.Status });
        }
    }

    public class PredictionRecordConfiguration : IEntityTypeConfiguration<PredictionRecord>
    {
        public void Configure(EntityTypeBuilder<PredictionRecord> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.PredictedScore).HasPrecision(5, 2);
            builder.Property(x => x.ConfidenceScore).HasPrecision(5, 4);
            builder.Property(x => x.ModelName).HasMaxLength(128);
            builder.Property(x => x.ModelVersion).HasMaxLength(64);

            builder.HasOne(x => x.Student)
                .WithMany(s => s.predictionRecords)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.StudentId, x.Kind, x.PredictedAt });
        }
    }
}
