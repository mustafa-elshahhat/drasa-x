using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class Message : AuditableEntity<string>
    {
        public string ConversationId { get; set; }
        public Conversation Conversation { get; set; }
        public string SenderId { get; set; }
        public ApplicationUser Sender { get; set; }
        public MessageType Type { get; set; } = MessageType.Text;
        public string Body { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? EditedAt { get; set; }
        public ICollection<MessageAttachment> Attachments { get; set; } = new HashSet<MessageAttachment>();
        public ICollection<MessageReadReceipt> ReadReceipts { get; set; } = new HashSet<MessageReadReceipt>();
    }
}
