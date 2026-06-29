using DerasaX.Domain.Entities.Base;

namespace DerasaX.Domain.Entities.Models
{
    public class FeatureFlag : PlatformEntity<string>
    {
        public string Key { get; set; }
        public bool IsEnabled { get; set; }
        public string? TargetTenantId { get; set; }
        public string? Description { get; set; }
    }
}
