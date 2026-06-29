using DerasaX.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DerasaX.Infrastructure.configuration
{
    public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
    {
        public void Configure(EntityTypeBuilder<Quiz> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Title).HasMaxLength(256);
            builder.Property(x => x.ApprovedByTeacherId).HasMaxLength(450);
            builder.Property(x => x.ReviewedByTeacherId).HasMaxLength(450);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });
            builder.HasIndex(x => new { x.TenantId, x.Status });
        }
    }

    public class QuestionConfiguration : IEntityTypeConfiguration<Question>
    {
        public void Configure(EntityTypeBuilder<Question> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.QuizId).IsRequired().HasMaxLength(450);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });

            // Same-tenant: a question must belong to a quiz of the same tenant.
            builder.HasOne(x => x.Quiz)
                .WithMany(q => q.Questions)
                .HasForeignKey(x => new { x.TenantId, x.QuizId })
                .HasPrincipalKey(q => new { q.TenantId, q.Id })
                .OnDelete(DeleteBehavior.Cascade); // a quiz owns its questions
        }
    }

    public class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
    {
        public void Configure(EntityTypeBuilder<QuestionOption> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.QuestionId).IsRequired().HasMaxLength(450);

            builder.HasOne(x => x.Question)
                .WithMany(q => q.Options)
                .HasForeignKey(x => new { x.TenantId, x.QuestionId })
                .HasPrincipalKey(q => new { q.TenantId, q.Id })
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class QuizGenerationConfiguration : IEntityTypeConfiguration<QuizGeneration>
    {
        public void Configure(EntityTypeBuilder<QuizGeneration> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.QuizId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.AiProvider).HasMaxLength(64);
            builder.Property(x => x.AiModel).HasMaxLength(128);
            builder.Property(x => x.ModelVersion).HasMaxLength(64);
            builder.Property(x => x.PromptVersion).HasMaxLength(64);
            builder.Property(x => x.CorrelationId).HasMaxLength(128);
            builder.Property(x => x.ErrorCategory).HasMaxLength(128);
            builder.Property(x => x.ReviewedByTeacherId).HasMaxLength(450);

            builder.HasOne(x => x.Quiz)
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.QuizId })
                .HasPrincipalKey(q => new { q.TenantId, q.Id })
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class QuizSubmissionConfiguration : IEntityTypeConfiguration<QuizSubmission>
    {
        public void Configure(EntityTypeBuilder<QuizSubmission> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.QuizId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.AssignmentId).HasMaxLength(450);
            builder.Property(x => x.GradedByTeacherId).HasMaxLength(450);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });

            // Same-tenant submission<->quiz. Quiz history must never be cascade-deleted
            // with a quiz: keep grades even if a quiz is archived (Restrict).
            builder.HasOne(x => x.Quiz)
                .WithMany(q => q.QuizSubmissions)
                .HasForeignKey(x => new { x.TenantId, x.QuizId })
                .HasPrincipalKey(q => new { q.TenantId, q.Id })
                .OnDelete(DeleteBehavior.Restrict);

            // Student is a user: simple FK + trigger enforces same-tenant.
            builder.HasOne(x => x.Student)
                .WithMany(s => s.quizSubmissions)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.StudentId, x.QuizId, x.AttemptNumber }).IsUnique();
            builder.HasIndex(x => new { x.TenantId, x.AssignmentId });
        }
    }

    public class SubmissionAnswerConfiguration : IEntityTypeConfiguration<SubmissionAnswer>
    {
        public void Configure(EntityTypeBuilder<SubmissionAnswer> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.QuestionId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.QuizSubmissionId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.GradedByTeacherId).HasMaxLength(450);

            builder.HasOne(x => x.QuizSubmission)
                .WithMany(s => s.SubmissionAnswers)
                .HasForeignKey(x => new { x.TenantId, x.QuizSubmissionId })
                .HasPrincipalKey(s => new { s.TenantId, s.Id })
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Question)
                .WithMany(q => q.SubmissionAnswers)
                .HasForeignKey(x => new { x.TenantId, x.QuestionId })
                .HasPrincipalKey(q => new { q.TenantId, q.Id })
                .OnDelete(DeleteBehavior.Restrict);

            // SelectedOption stays a simple optional FK (option belongs to the question).
            builder.HasOne(x => x.SelectedOption)
                .WithMany(o => o.SubmissionAnswers)
                .HasForeignKey(x => x.SelectedOptionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
    {
        public void Configure(EntityTypeBuilder<Assignment> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Title).IsRequired().HasMaxLength(256);
            builder.Property(x => x.AssignedByTeacherId).HasMaxLength(450);
            builder.Property(x => x.QuizId).HasMaxLength(450);
            builder.Property(x => x.SubjectId).HasMaxLength(450);
            builder.Property(x => x.LessonId).HasMaxLength(450);
            builder.Property(x => x.MaxScore).HasPrecision(9, 2);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });

            builder.HasOne(x => x.Quiz)
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.QuizId })
                .HasPrincipalKey(q => new { q.TenantId, q.Id })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Subject)
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SubjectId })
                .HasPrincipalKey(s => new { s.TenantId, s.Id })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.Status });
        }
    }

    public class AssignmentTargetConfiguration : IEntityTypeConfiguration<AssignmentTarget>
    {
        public void Configure(EntityTypeBuilder<AssignmentTarget> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.AssignmentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.SchoolClassId).HasMaxLength(450);
            builder.Property(x => x.StudentId).HasMaxLength(450);
            builder.Property(x => x.GradeId).HasMaxLength(450);

            builder.HasOne(x => x.Assignment)
                .WithMany(a => a.Targets)
                .HasForeignKey(x => new { x.TenantId, x.AssignmentId })
                .HasPrincipalKey(a => new { a.TenantId, a.Id })
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.SchoolClass)
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SchoolClassId })
                .HasPrincipalKey(c => new { c.TenantId, c.Id })
                .OnDelete(DeleteBehavior.Restrict);

            // Student is a user: simple optional FK + trigger enforces same-tenant.
            builder.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.AssignmentId, x.TargetType });
        }
    }

    public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
    {
        public void Configure(EntityTypeBuilder<Lesson> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });
        }
    }
}
