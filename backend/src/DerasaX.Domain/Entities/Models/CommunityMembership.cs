using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class CommunityMembership : AuditableEntity<string>
    {
        public string CommunityId { get; set; } = null!;
        public Community Community { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public CommunityMemberRole Role { get; set; } = CommunityMemberRole.Member;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
