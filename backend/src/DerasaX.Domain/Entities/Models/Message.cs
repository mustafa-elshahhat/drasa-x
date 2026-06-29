using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class Message : AuditableEntity<string>
    {
        public string ConversationId { get; set; } = null!;
        public Conversation Conversation { get; set; } = null!;
        public string SenderId { get; set; } = null!;
        public ApplicationUser Sender { get; set; } = null!;
        public MessageType Type { get; set; } = MessageType.Text;
        public string Body { get; set; } = null!;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? EditedAt { get; set; }
        public ICollection<MessageAttachment> Attachments { get; set; } = new HashSet<MessageAttachment>();
        public ICollection<MessageReadReceipt> ReadReceipts { get; set; } = new HashSet<MessageReadReceipt>();
    }
}
