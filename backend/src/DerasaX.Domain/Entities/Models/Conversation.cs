using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class Conversation : AuditableEntity<string>
    {
        public ConversationType Type { get; set; }
        public string? Subject { get; set; }
        public bool IsClosed { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }
        public ICollection<ConversationParticipant> Participants { get; set; } = new HashSet<ConversationParticipant>();
        public ICollection<Message> Messages { get; set; } = new HashSet<Message>();
    }
}
