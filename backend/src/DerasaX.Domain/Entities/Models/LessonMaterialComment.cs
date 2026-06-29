using DerasaX.Domain.Entities.Base;

namespace DerasaX.Domain.Entities.Models
{
    /// <summary>
    /// A comment posted by a tenant member on a specific lesson resource (<see cref="LessonMaterial"/>).
    /// Distinct from community <see cref="PostComment"/>s: these are anchored to curriculum resources.
    /// Tenant-owned and soft-deletable (moderation = soft delete).
    /// </summary>
    public class LessonMaterialComment : AuditableEntity<string>
    {
        public string MaterialId { get; set; } = string.Empty;
        public LessonMaterial? Material { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        public string Body { get; set; } = string.Empty;
    }
}
