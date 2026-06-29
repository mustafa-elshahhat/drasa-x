using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class SystemSetting : PlatformEntity<string>
    {
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
        public SettingValueType ValueType { get; set; } = SettingValueType.String;
        public string? Description { get; set; }
        public bool IsSecret { get; set; }
    }
}
