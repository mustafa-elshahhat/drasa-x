using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class Community : AuditableEntity<string>
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public CommunityVisibility Visibility { get; set; } = CommunityVisibility.TenantOnly;
        public string? SchoolClassId { get; set; }
        public SchoolClass? SchoolClass { get; set; }
        /// <summary>
        /// Phase 14 (closure) — optional grade-eligibility gate. When set, only students whose
        /// <see cref="Student.GradeId"/> matches may self-join; staff/admins are unaffected.
        /// </summary>
        public string? EligibleGradeId { get; set; }
        public Grade? EligibleGrade { get; set; }
        public ICollection<CommunityMembership> Memberships { get; set; } = new HashSet<CommunityMembership>();
        public ICollection<Post> Posts { get; set; } = new HashSet<Post>();
    }
}
