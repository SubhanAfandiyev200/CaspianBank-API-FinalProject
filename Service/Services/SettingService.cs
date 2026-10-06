using Repository.Repositories.Interfaces;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class SettingService : ISettingService
    {
        private readonly ISettingRepository _settingRepo;

        public SettingService(ISettingRepository settingRepo)
        {
            _settingRepo = settingRepo;
        }

        public async Task<Dictionary<string, string>> GetAllAsync()
        {
            var settings = await _settingRepo.GetAllAsync();

            // Açar bazada unikaldır (unique index), ona görə lüğətə çevirmək təhlükəsizdir
            return settings.ToDictionary(s => s.Key, s => s.Value);
        }
    }
}
