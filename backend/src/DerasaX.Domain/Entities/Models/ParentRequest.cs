using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class ParentRequest : AuditableEntity<string>
    {
        public string ParentId { get; set; }
        public Parent Parent { get; set; }
        public string StudentId { get; set; }
        public Student Student { get; set; }
        public ParentRequestType Type { get; set; }
        public ParentRequestStatus Status { get; set; } = ParentRequestStatus.Open;
        public string Title { get; set; }
        public string Body { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        /// <summary>Phase 16 — optional sensitive document the parent attached to the request.</summary>
        public string? FileRecordId { get; set; }
        public ICollection<ParentRequestResponse> Responses { get; set; } = new HashSet<ParentRequestResponse>();
    }
}
