using DerasaX.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DerasaX.Infrastructure.configuration
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.ActorUserId).HasMaxLength(450);
            builder.Property(x => x.EntityType).IsRequired().HasMaxLength(128);
            builder.Property(x => x.EntityId).HasMaxLength(450);
            builder.Property(x => x.CorrelationId).HasMaxLength(128);
            builder.Property(x => x.IpAddress).HasMaxLength(64);
            builder.Property(x => x.UserAgent).HasMaxLength(512);
            builder.HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => new { x.TenantId, x.EntityType, x.EntityId });
            builder.HasIndex(x => new { x.TenantId, x.OccurredAt });
        }
    }

    public class FileRecordConfiguration : IEntityTypeConfiguration<FileRecord>
    {
        public void Configure(EntityTypeBuilder<FileRecord> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.FileName).IsRequired().HasMaxLength(256);
            builder.Property(x => x.ContentType).IsRequired().HasMaxLength(128);
            builder.Property(x => x.StorageKey).IsRequired().HasMaxLength(1024);
            builder.Property(x => x.ChecksumSha256).HasMaxLength(64);
            builder.Property(x => x.UploadedByUserId).HasMaxLength(450);
            // Phase 16 — durable storage metadata.
            builder.Property(x => x.SafeStoredFileName).HasMaxLength(256);
            builder.Property(x => x.StorageProvider).HasMaxLength(32);
            builder.Property(x => x.StorageBucket).HasMaxLength(256);
            builder.Property(x => x.RelatedEntityType).HasMaxLength(64);
            builder.Property(x => x.RelatedEntityId).HasMaxLength(450);
            builder.Property(x => x.DeletedByUserId).HasMaxLength(450);
            builder.Property(x => x.ConsentReference).HasMaxLength(256);
            builder.HasOne(x => x.UploadedByUser).WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => new { x.TenantId, x.StorageKey }).IsUnique();
            builder.HasIndex(x => new { x.TenantId, x.Purpose });
            builder.HasIndex(x => new { x.TenantId, x.RelatedEntityType, x.RelatedEntityId });
        }
    }

    public class AiUsageRecordConfiguration : IEntityTypeConfiguration<AiUsageRecord>
    {
        public void Configure(EntityTypeBuilder<AiUsageRecord> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.UserId).HasMaxLength(450);
            builder.Property(x => x.Provider).IsRequired().HasMaxLength(64);
            builder.Property(x => x.Model).HasMaxLength(128);
            builder.Property(x => x.Cost).HasPrecision(12, 6);
            builder.Property(x => x.CorrelationId).HasMaxLength(128);
            builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => new { x.TenantId, x.Kind, x.UsedAt });
        }
    }

    public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
    {
        public void Configure(EntityTypeBuilder<SystemSetting> builder)
        {
            builder.Property(x => x.Key).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Value).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(1024);
            builder.HasIndex(x => x.Key).IsUnique();
        }
    }

    public class TenantSettingConfiguration : IEntityTypeConfiguration<TenantSetting>
    {
        public void Configure(EntityTypeBuilder<TenantSetting> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Key).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Value).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(1024);
            builder.HasIndex(x => new { x.TenantId, x.Key }).IsUnique();
        }
    }

    public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
    {
        public void Configure(EntityTypeBuilder<FeatureFlag> builder)
        {
            builder.Property(x => x.Key).IsRequired().HasMaxLength(128);
            builder.Property(x => x.TargetTenantId).HasMaxLength(450);
            builder.Property(x => x.Description).HasMaxLength(1024);
            builder.HasIndex(x => new { x.Key, x.TargetTenantId }).IsUnique();
        }
    }

    public class SupportRequestConfiguration : IEntityTypeConfiguration<SupportRequest>
    {
        public void Configure(EntityTypeBuilder<SupportRequest> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Message).IsRequired().HasMaxLength(4096);
            builder.Property(x => x.ResponseMessage).HasMaxLength(4096);
            builder.HasOne(x => x.User).WithMany(u => u.supportRequests).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => new { x.TenantId, x.Status, x.CreatedAt });
        }
    }
}
