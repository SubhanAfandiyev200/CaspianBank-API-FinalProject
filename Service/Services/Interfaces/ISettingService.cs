namespace Service.Services.Interfaces
{
    public interface ISettingService
    {
        // Bütün ayarlar açar-dəyər lüğəti kimi: { "SiteName": "Caspian Bank", ... }
        Task<Dictionary<string, string>> GetAllAsync();
    }
}
