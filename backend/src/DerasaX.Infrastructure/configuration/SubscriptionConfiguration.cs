using DerasaX.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DerasaX.Infrastructure.configuration
{
    public class SubscriptionPlanDefinitionConfiguration : IEntityTypeConfiguration<SubscriptionPlanDefinition>
    {
        public void Configure(EntityTypeBuilder<SubscriptionPlanDefinition> builder)
        {
            builder.Property(x => x.Code).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Description).HasMaxLength(1024);
            builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            builder.Property(x => x.Price).HasPrecision(18, 2);

            // Platform-global unique plan code.
            builder.HasIndex(x => x.Code).IsUnique();
        }
    }

    public class TenantSubscriptionConfiguration : IEntityTypeConfiguration<TenantSubscription>
    {
        public void Configure(EntityTypeBuilder<TenantSubscription> builder)
        {
            // Required so it can take part in the (TenantId, Id) alternate key that the
            // same-tenant composite FKs below reference.
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });

            builder.Property(x => x.PlanDefinitionId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.CancellationReason).HasMaxLength(512);

            // Plan catalog is platform-owned; never cascade-delete a plan into tenant data.
            builder.HasOne(x => x.PlanDefinition)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(x => x.PlanDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.Status });
            builder.HasIndex(x => new { x.TenantId, x.ExpiresAt });
        }
    }

    public class SubscriptionRenewalConfiguration : IEntityTypeConfiguration<SubscriptionRenewal>
    {
        public void Configure(EntityTypeBuilder<SubscriptionRenewal> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.TenantSubscriptionId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Notes).HasMaxLength(1024);

            // Same-tenant composite FK: a renewal's tenant MUST equal the renewed
            // subscription's tenant (both share the TenantId column), enforced by the DB.
            builder.HasOne(x => x.TenantSubscription)
                .WithMany(s => s.Renewals)
                .HasForeignKey(x => new { x.TenantId, x.TenantSubscriptionId })
                .HasPrincipalKey(s => new { s.TenantId, s.Id })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.TenantSubscriptionId });
        }
    }

    public class TenantUsageCounterConfiguration : IEntityTypeConfiguration<TenantUsageCounter>
    {
        public void Configure(EntityTypeBuilder<TenantUsageCounter> builder)
        {
            builder.HasIndex(x => new { x.TenantId, x.PeriodStart });
        }
    }
}
