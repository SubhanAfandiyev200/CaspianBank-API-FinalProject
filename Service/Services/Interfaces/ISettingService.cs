using Service.Helpers.DTOs.Settings;

namespace Service.Services.Interfaces
{
    // Açarlar kodda sabitdir (SettingKeys), ona görə admin yalnız siyahını görür, birini açır (GetByIdAsync) və dəyərini dəyişir (əlavə/silmə yoxdur)
    public interface ISettingService
    {
        // İctimai: bütün ayarlar açar-dəyər lüğəti kimi: { "CompanyName": "Caspian Bank", ... }
        Task<Dictionary<string, string>> GetAllUIAsync();

        // Admin
        Task<List<SettingDto>> GetAllAsync();
        Task<SettingDto> GetByIdAsync(int id);
        Task UpdateAsync(int id, UpdateSettingDto model);
        Task UpdateLogoAsync(int id, UpdateSettingLogoDto model);
    }
}
