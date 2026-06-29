using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class AuditLog : AuditableEntity<string>
    {
        public string? ActorUserId { get; set; }
        public ApplicationUser? ActorUser { get; set; }
        public AuditActionType Action { get; set; }
        public string EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? CorrelationId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? MetadataJson { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
