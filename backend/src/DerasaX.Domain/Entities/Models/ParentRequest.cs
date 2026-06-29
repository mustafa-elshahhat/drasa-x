using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class ParentRequest : AuditableEntity<string>
    {
        public string ParentId { get; set; } = null!;
        public Parent Parent { get; set; } = null!;
        public string StudentId { get; set; } = null!;
        public Student Student { get; set; } = null!;
        public ParentRequestType Type { get; set; }
        public ParentRequestStatus Status { get; set; } = ParentRequestStatus.Open;
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        /// <summary>Phase 16 — optional sensitive document the parent attached to the request.</summary>
        public string? FileRecordId { get; set; }
        public ICollection<ParentRequestResponse> Responses { get; set; } = new HashSet<ParentRequestResponse>();
    }
}
