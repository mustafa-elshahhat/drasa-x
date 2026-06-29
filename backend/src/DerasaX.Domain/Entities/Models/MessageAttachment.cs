using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class MessageAttachment : AuditableEntity<string>
    {
        public string MessageId { get; set; } = null!;
        public Message Message { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string Url { get; set; } = null!;
        public AttachmentType Type { get; set; }
        public long? SizeBytes { get; set; }
        /// <summary>Phase 16 — durable file backing this attachment (vs. a legacy external URL).</summary>
        public string? FileRecordId { get; set; }
    }
}
