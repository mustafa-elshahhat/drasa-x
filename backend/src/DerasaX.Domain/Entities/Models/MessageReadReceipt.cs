using DerasaX.Domain.Entities.Base;

namespace DerasaX.Domain.Entities.Models
{
    public class MessageReadReceipt : AuditableEntity<string>
    {
        public string MessageId { get; set; } = null!;
        public Message Message { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    }
}
