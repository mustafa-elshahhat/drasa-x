using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class PostReport : AuditableEntity<string>
    {
        public string PostId { get; set; }
        public Post Post { get; set; }
        public string ReportedByUserId { get; set; }
        public ApplicationUser ReportedByUser { get; set; }
        public string Reason { get; set; }
        public ReportStatus Status { get; set; } = ReportStatus.Open;
    }
}
