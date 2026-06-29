using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;

namespace DerasaX.Domain.Entities.Models
{
    public class SystemSetting : PlatformEntity<string>
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public SettingValueType ValueType { get; set; } = SettingValueType.String;
        public string? Description { get; set; }
        public bool IsSecret { get; set; }
    }
}
