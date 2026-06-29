using DerasaX.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DerasaX.Infrastructure.configuration
{
    /// <summary>
    /// Phase 5 closure — submissions to non-quiz assignments (homework lifecycle). Same-tenant
    /// integrity via composite FK to the assignment; one submission per student per assignment.
    /// </summary>
    public class AssignmentSubmissionConfiguration : IEntityTypeConfiguration<AssignmentSubmission>
    {
        public void Configure(EntityTypeBuilder<AssignmentSubmission> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.AssignmentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Content).HasMaxLength(8192);
            builder.Property(x => x.AttachmentFileId).HasMaxLength(450);
            builder.Property(x => x.Feedback).HasMaxLength(4096);
            builder.Property(x => x.GradedByTeacherId).HasMaxLength(450);
            builder.Property(x => x.Score).HasPrecision(9, 2);

            builder.HasOne(x => x.Assignment).WithMany()
                .HasForeignKey(x => new { x.TenantId, x.AssignmentId })
                .HasPrincipalKey(a => new { a.TenantId, a.Id })
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.Student).WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.AssignmentId, x.StudentId }).IsUnique();
        }
    }
}
