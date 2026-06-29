using DerasaX.Domain.Entities.Base;

namespace DerasaX.Domain.Entities.Models
{
    public class PostComment : AuditableEntity<string>
    {
        public string PostId { get; set; }
        public Post Post { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public string Body { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
