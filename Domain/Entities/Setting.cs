using Domain.Common;

namespace Domain.Entities
{
    // Saytın ümumi ayarları: açar-dəyər (məs. "LogoOnDark" = "/images/settings/logo.png", "SiteName" = "Caspian Bank").
    // Yeni ayar əlavə etmək üçün yeni cədvəl/migration lazım deyil, sadəcə yeni sətir yazılır.
    public class Setting : BaseEntity
    {
        public string Key { get; set; } = string.Empty;     // unikal
        public string Value { get; set; } = string.Empty;
    }
}
