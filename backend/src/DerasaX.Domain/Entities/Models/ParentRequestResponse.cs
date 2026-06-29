using DerasaX.Domain.Entities.Base;

namespace DerasaX.Domain.Entities.Models
{
    public class ParentRequestResponse : AuditableEntity<string>
    {
        public string ParentRequestId { get; set; } = null!;
        public ParentRequest ParentRequest { get; set; } = null!;
        public string ResponderId { get; set; } = null!;
        public ApplicationUser Responder { get; set; } = null!;
        public string Body { get; set; } = null!;
        public DateTime RespondedAt { get; set; } = DateTime.UtcNow;
        /// <summary>Phase 16 — optional sensitive document the responder (staff) attached.</summary>
        public string? FileRecordId { get; set; }
    }
}
