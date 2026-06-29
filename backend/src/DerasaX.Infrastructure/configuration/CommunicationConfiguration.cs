using DerasaX.Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DerasaX.Infrastructure.configuration
{
    public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
    {
        public void Configure(EntityTypeBuilder<Conversation> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Subject).HasMaxLength(256);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });
            builder.HasIndex(x => new { x.TenantId, x.Type, x.IsClosed });
        }
    }

    public class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
    {
        public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.ConversationId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);

            builder.HasOne(x => x.Conversation)
                .WithMany(c => c.Participants)
                .HasForeignKey(x => new { x.TenantId, x.ConversationId })
                .HasPrincipalKey(c => new { c.TenantId, c.Id })
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.ConversationId, x.UserId }).IsUnique();
        }
    }

    public class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.ConversationId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.SenderId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Body).IsRequired();
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });

            builder.HasOne(x => x.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(x => new { x.TenantId, x.ConversationId })
                .HasPrincipalKey(c => new { c.TenantId, c.Id })
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Sender)
                .WithMany()
                .HasForeignKey(x => x.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.ConversationId, x.SentAt });
        }
    }

    public class MessageAttachmentConfiguration : IEntityTypeConfiguration<MessageAttachment>
    {
        public void Configure(EntityTypeBuilder<MessageAttachment> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.MessageId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.FileName).IsRequired().HasMaxLength(256);
            builder.Property(x => x.Url).IsRequired().HasMaxLength(2048);
            builder.Property(x => x.FileRecordId).HasMaxLength(450);

            builder.HasOne(x => x.Message)
                .WithMany(m => m.Attachments)
                .HasForeignKey(x => new { x.TenantId, x.MessageId })
                .HasPrincipalKey(m => new { m.TenantId, m.Id })
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class MessageReadReceiptConfiguration : IEntityTypeConfiguration<MessageReadReceipt>
    {
        public void Configure(EntityTypeBuilder<MessageReadReceipt> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.MessageId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);

            builder.HasOne(x => x.Message)
                .WithMany(m => m.ReadReceipts)
                .HasForeignKey(x => new { x.TenantId, x.MessageId })
                .HasPrincipalKey(m => new { m.TenantId, m.Id })
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.MessageId, x.UserId }).IsUnique();
        }
    }

    public class ParentRequestConfiguration : IEntityTypeConfiguration<ParentRequest>
    {
        public void Configure(EntityTypeBuilder<ParentRequest> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.ParentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StudentId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Title).IsRequired().HasMaxLength(256);
            builder.Property(x => x.Body).IsRequired().HasMaxLength(4096);
            builder.Property(x => x.FileRecordId).HasMaxLength(450);
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });

            builder.HasOne(x => x.Parent)
                .WithMany()
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.StudentId, x.Status });
        }
    }

    public class ParentRequestResponseConfiguration : IEntityTypeConfiguration<ParentRequestResponse>
    {
        public void Configure(EntityTypeBuilder<ParentRequestResponse> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.ParentRequestId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.ResponderId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Body).IsRequired().HasMaxLength(4096);
            builder.Property(x => x.FileRecordId).HasMaxLength(450);

            builder.HasOne(x => x.ParentRequest)
                .WithMany(r => r.Responses)
                .HasForeignKey(x => new { x.TenantId, x.ParentRequestId })
                .HasPrincipalKey(r => new { r.TenantId, r.Id })
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Responder)
                .WithMany()
                .HasForeignKey(x => x.ResponderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class SuggestionConfiguration : IEntityTypeConfiguration<Suggestion>
    {
        public void Configure(EntityTypeBuilder<Suggestion> builder)
        {
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.SubmittedByUserId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.ReviewedByUserId).HasMaxLength(450);
            builder.Property(x => x.Title).IsRequired().HasMaxLength(256);
            builder.Property(x => x.Body).IsRequired().HasMaxLength(4096);
            builder.Property(x => x.ReviewNotes).HasMaxLength(2048);

            builder.HasOne(x => x.SubmittedByUser)
                .WithMany()
                .HasForeignKey(x => x.SubmittedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TenantId, x.Status, x.SubmittedAt });
        }
    }
}
