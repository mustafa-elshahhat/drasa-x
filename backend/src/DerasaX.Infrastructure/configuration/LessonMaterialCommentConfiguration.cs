using DerasaX.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DerasaX.Infrastructure.configuration
{
    /// <summary>
    /// Phase 5 closure — comments on lesson resources. Tenant-scoped; a simple FK to the
    /// owning <see cref="LessonMaterial"/> (cascade) and to the author (restrict).
    /// </summary>
    public class LessonMaterialCommentConfiguration : IEntityTypeConfiguration<LessonMaterialComment>
    {
        public void Configure(EntityTypeBuilder<LessonMaterialComment> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.MaterialId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Body).IsRequired().HasMaxLength(2048);

            builder.HasOne(x => x.Material).WithMany()
                .HasForeignKey(x => x.MaterialId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.MaterialId, x.CreatedAt });
        }
    }
}
