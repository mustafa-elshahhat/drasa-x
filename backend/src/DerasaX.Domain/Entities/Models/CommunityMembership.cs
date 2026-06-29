using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class CommunityMembership : AuditableEntity<string>
    {
        public string CommunityId { get; set; }
        public Community Community { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public CommunityMemberRole Role { get; set; } = CommunityMemberRole.Member;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
