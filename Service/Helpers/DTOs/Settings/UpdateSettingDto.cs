using System.Text.Json.Serialization;

namespace Service.Helpers.DTOs.Settings
{
    // Yalnız dəyər dəyişir, açarın adı yox. Logo mətn deyil, şəkil yoludur: o UpdateSettingLogoDto ilə dəyişir
    public class UpdateSettingDto
    {
        public string? Value { get; set; }

        // İstəkdən gəlmir: servis ayarı tapıb açarı özü yazır ki, validator açara görə (Email, telefon, uzunluq) yoxlasın
        [JsonIgnore]
        public string Key { get; set; } = string.Empty;
    }
}
