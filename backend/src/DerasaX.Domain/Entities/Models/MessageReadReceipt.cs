using DerasaX.Domain.Entities.Base;

namespace DerasaX.Domain.Entities.Models
{
    public class MessageReadReceipt : AuditableEntity<string>
    {
        public string MessageId { get; set; }
        public Message Message { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    }
}
