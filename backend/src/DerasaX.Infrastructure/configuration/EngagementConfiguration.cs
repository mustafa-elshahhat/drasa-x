using DerasaX.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DerasaX.Infrastructure.configuration
{
    public class CommunityConfiguration : IEntityTypeConfiguration<Community>
    {
        public void Configure(EntityTypeBuilder<Community> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Description).HasMaxLength(1024);
            builder.Property(x => x.SchoolClassId).HasMaxLength(450);
            builder.Property(x => x.EligibleGradeId).HasMaxLength(450);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });
            builder.HasOne(x => x.SchoolClass).WithMany()
                .HasForeignKey(x => new { x.TenantId, x.SchoolClassId })
                .HasPrincipalKey(c => new { c.TenantId, c.Id })
                .OnDelete(DeleteBehavior.Restrict);
            // Phase 14 (closure) — optional grade-eligibility gate. Simple FK to grades(Id), mirroring
            // Student.GradeId; clearing the grade on delete leaves the community open (SetNull).
            builder.HasOne(x => x.EligibleGrade).WithMany()
                .HasForeignKey(x => x.EligibleGradeId)
                .OnDelete(DeleteBehavior.SetNull);
            builder.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            builder.HasIndex(x => new { x.TenantId, x.EligibleGradeId });
        }
    }

    public class CommunityMembershipConfiguration : IEntityTypeConfiguration<CommunityMembership>
    {
        public void Configure(EntityTypeBuilder<CommunityMembership> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.CommunityId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            builder.HasOne(x => x.Community).WithMany(c => c.Memberships)
                .HasForeignKey(x => new { x.TenantId, x.CommunityId })
                .HasPrincipalKey(c => new { c.TenantId, c.Id })
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => new { x.TenantId, x.CommunityId, x.UserId }).IsUnique();
        }
    }

    public class PostConfiguration : IEntityTypeConfiguration<Post>
    {
        public void Configure(EntityTypeBuilder<Post> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.CommunityId).HasMaxLength(450);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });
            builder.HasOne(x => x.Community).WithMany(c => c.Posts)
                .HasForeignKey(x => new { x.TenantId, x.CommunityId })
                .HasPrincipalKey(c => new { c.TenantId, c.Id })
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.User).WithMany(u => u.posts).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => new { x.TenantId, x.CommunityId, x.CreatedAt });
        }
    }

    public class PostCommentConfiguration : IEntityTypeConfiguration<PostComment>
    {
        public void Configure(EntityTypeBuilder<PostComment> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.PostId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Body).IsRequired().HasMaxLength(2048);
            builder.HasOne(x => x.Post).WithMany(p => p.Comments)
                .HasForeignKey(x => new { x.TenantId, x.PostId })
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class PostReportConfiguration : IEntityTypeConfiguration<PostReport>
    {
        public void Configure(EntityTypeBuilder<PostReport> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.PostId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.ReportedByUserId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Reason).IsRequired().HasMaxLength(1024);
            builder.HasOne(x => x.Post).WithMany(p => p.Reports)
                .HasForeignKey(x => new { x.TenantId, x.PostId })
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.ReportedByUser).WithMany().HasForeignKey(x => x.ReportedByUserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => new { x.TenantId, x.Status });
        }
    }

    public class CompetitionConfiguration : IEntityTypeConfiguration<Competition>
    {
        public void Configure(EntityTypeBuilder<Competition> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Title).IsRequired().HasMaxLength(256);
            builder.Property(x => x.Description).HasMaxLength(2048);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });
            builder.HasIndex(x => new { x.TenantId, x.Status, x.StartsAt });
        }
    }

    public class CompetitionEntryConfiguration : IEntityTypeConfiguration<CompetitionEntry>
    {
        public void Configure(EntityTypeBuilder<CompetitionEntry> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.CompetitionId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });
            builder.HasOne(x => x.Competition).WithMany(c => c.Entries)
                .HasForeignKey(x => new { x.TenantId, x.CompetitionId })
                .HasPrincipalKey(c => new { c.TenantId, c.Id })
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => new { x.TenantId, x.CompetitionId, x.StudentId }).IsUnique();
        }
    }

    public class CompetitionScoreConfiguration : IEntityTypeConfiguration<CompetitionScore>
    {
        public void Configure(EntityTypeBuilder<CompetitionScore> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.CompetitionEntryId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Score).HasPrecision(9, 2);
            builder.HasOne(x => x.CompetitionEntry).WithMany(e => e.Scores)
                .HasForeignKey(x => new { x.TenantId, x.CompetitionEntryId })
                .HasPrincipalKey(e => new { e.TenantId, e.Id })
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class CompetitionSubmissionConfiguration : IEntityTypeConfiguration<CompetitionSubmission>
    {
        public void Configure(EntityTypeBuilder<CompetitionSubmission> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.CompetitionId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Content).IsRequired().HasMaxLength(8192);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });
            builder.HasOne(x => x.Competition).WithMany(c => c.Submissions)
                .HasForeignKey(x => new { x.TenantId, x.CompetitionId })
                .HasPrincipalKey(c => new { c.TenantId, c.Id })
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            // One durable submission per student per competition (resubmission updates it in place).
            builder.HasIndex(x => new { x.TenantId, x.CompetitionId, x.StudentId }).IsUnique();
        }
    }

    public class LeaderboardEntryConfiguration : IEntityTypeConfiguration<LeaderboardEntry>
    {
        public void Configure(EntityTypeBuilder<LeaderboardEntry> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.CompetitionId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Score).HasPrecision(9, 2);
            builder.HasOne(x => x.Competition).WithMany()
                .HasForeignKey(x => new { x.TenantId, x.CompetitionId })
                .HasPrincipalKey(c => new { c.TenantId, c.Id })
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => new { x.TenantId, x.CompetitionId, x.Rank }).IsUnique();
        }
    }

    public class BadgeConfiguration : IEntityTypeConfiguration<Badge>
    {
        public void Configure(EntityTypeBuilder<Badge> builder)
        {
            builder.Property(x => x.Code).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Description).HasMaxLength(1024);
            builder.Property(x => x.IconUrl).HasMaxLength(2048);
            builder.HasIndex(x => x.Code).IsUnique();
        }
    }

    public class StudentBadgeConfiguration : IEntityTypeConfiguration<StudentBadge>
    {
        public void Configure(EntityTypeBuilder<StudentBadge> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.BadgeId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.AwardedReason).HasMaxLength(1024);
            builder.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Badge).WithMany().HasForeignKey(x => x.BadgeId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => new { x.TenantId, x.StudentId, x.BadgeId }).IsUnique();
        }
    }

    public class StudentStreakConfiguration : IEntityTypeConfiguration<StudentStreak>
    {
        public void Configure(EntityTypeBuilder<StudentStreak> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);
            builder.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => new { x.TenantId, x.StudentId }).IsUnique();
        }
    }

    public class OfficeHourSessionConfiguration : IEntityTypeConfiguration<OfficeHourSession>
    {
        public void Configure(EntityTypeBuilder<OfficeHourSession> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.TeacherId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Title).IsRequired().HasMaxLength(256);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });
            builder.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => new { x.TenantId, x.TeacherId, x.StartsAt });
        }
    }

    public class OfficeHourBookingConfiguration : IEntityTypeConfiguration<OfficeHourBooking>
    {
        public void Configure(EntityTypeBuilder<OfficeHourBooking> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.OfficeHourSessionId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Notes).HasMaxLength(1024);
            builder.HasOne(x => x.OfficeHourSession).WithMany(s => s.Bookings)
                .HasForeignKey(x => new { x.TenantId, x.OfficeHourSessionId })
                .HasPrincipalKey(s => new { s.TenantId, s.Id })
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => new { x.TenantId, x.OfficeHourSessionId, x.StudentId }).IsUnique();
        }
    }

    // ---- Phase 14: gamification points ledger ----

    public class StudentPointTransactionConfiguration : IEntityTypeConfiguration<StudentPointTransaction>
    {
        public void Configure(EntityTypeBuilder<StudentPointTransaction> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Reason).IsRequired().HasMaxLength(256);
            builder.Property(x => x.SourceId).HasMaxLength(450);
            builder.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(200);
            builder.Property(x => x.GamificationRuleId).HasMaxLength(450);
            builder.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            // Anti-abuse: the same real-world event can never award points twice within a tenant.
            builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
            // Balance/ledger reads are by (tenant, student).
            builder.HasIndex(x => new { x.TenantId, x.StudentId, x.AwardedAt });
        }
    }

    public class GamificationRuleConfiguration : IEntityTypeConfiguration<GamificationRule>
    {
        public void Configure(EntityTypeBuilder<GamificationRule> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Code).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Description).HasMaxLength(1024);
            builder.Property(x => x.BadgeId).HasMaxLength(450);
            builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        }
    }
}
