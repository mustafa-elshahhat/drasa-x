using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class PostReport : AuditableEntity<string>
    {
        public string PostId { get; set; } = null!;
        public Post Post { get; set; } = null!;
        public string ReportedByUserId { get; set; } = null!;
        public ApplicationUser ReportedByUser { get; set; } = null!;
        public string Reason { get; set; } = null!;
        public ReportStatus Status { get; set; } = ReportStatus.Open;
    }
}
