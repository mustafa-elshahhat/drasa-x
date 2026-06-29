using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class ConversationParticipant : AuditableEntity<string>
    {
        public string ConversationId { get; set; }
        public Conversation Conversation { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public ConversationParticipantRole Role { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LeftAt { get; set; }
    }
}
