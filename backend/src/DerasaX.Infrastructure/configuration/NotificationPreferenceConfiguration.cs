using DerasaX.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DerasaX.Infrastructure.configuration
{
    public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
    {
        public void Configure(EntityTypeBuilder<NotificationPreference> builder)
        {
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);

            // One row per (tenant, user, category). The tenant FK/index is added generically in
            // OnModelCreating for every IMustHaveTenant entity; this enforces the upsert key.
            builder.HasIndex(x => new { x.TenantId, x.UserId, x.Category }).IsUnique();
            builder.HasIndex(x => x.UserId);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
