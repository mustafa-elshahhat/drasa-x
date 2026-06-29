using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class Badge : PlatformEntity<string>
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public BadgeType Type { get; set; }
        public string? IconUrl { get; set; }
    }
}
