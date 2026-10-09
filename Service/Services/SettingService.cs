using Domain.Constants;
using Domain.Entities;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Settings;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class SettingService : ISettingService
    {
        private const string Folder = "settings";

        private readonly ISettingRepository _settingRepo;
        private readonly IFileService _fileService;
        private readonly IValidator<UpdateSettingDto> _updateValidator;
        private readonly IValidator<UpdateSettingLogoDto> _logoValidator;

        public SettingService(ISettingRepository settingRepo,
                              IFileService fileService,
                              IValidator<UpdateSettingDto> updateValidator,
                              IValidator<UpdateSettingLogoDto> logoValidator)
        {
            _settingRepo = settingRepo;
            _fileService = fileService;
            _updateValidator = updateValidator;
            _logoValidator = logoValidator;
        }

        // Admin siyahısı: Id sırası ilə
        public async Task<List<SettingDto>> GetAllAsync()
        {
            var settings = await _settingRepo.GetAllAsync();
            return settings.OrderBy(m => m.Id).Select(ToDto).ToList();
        }

        public async Task<Dictionary<string, string>> GetAllUIAsync()
        {
            var settings = await _settingRepo.GetAllAsync();

            // Açar bazada unikaldır (unique index), ona görə lüğətə çevirmək təhlükəsizdir
            return settings.ToDictionary(s => s.Key, s => s.Value);
        }

        public async Task<SettingDto> GetByIdAsync(int id)
        {
            var setting = await _settingRepo.GetByIdAsync(id);
            if (setting is null) throw new NotFoundException();
            return ToDto(setting);
        }

        public async Task UpdateAsync(int id, UpdateSettingDto model)
        {
            var setting = await _settingRepo.GetByIdAsync(id);
            if (setting is null) throw new NotFoundException();

            // Logo şəkil yoludur: mətnlə yazılsa səhv yol saytda loqonu sındırar
            if (setting.Key == SettingKeys.Logo)
            {
                throw new BadRequestException("The logo is changed by uploading an image.");
            }

            // Açarı biz yazırıq (istəkdən gəlmir), validator ona görə yoxlayır
            model.Key = setting.Key;
            await _updateValidator.EnsureValidAsync(model);

            // Modeldəki dəyər tapılan obyektə köçürülməsə, UpdateAsync köhnə dəyəri yenidən yazar
            setting.Value = model.Value!.Trim();
            await _settingRepo.UpdateAsync(setting);
        }

        public async Task UpdateLogoAsync(int id, UpdateSettingLogoDto model)
        {
            await _logoValidator.EnsureValidAsync(model);

            var setting = await _settingRepo.GetByIdAsync(id);
            if (setting is null) throw new NotFoundException();
            if (setting.Key != SettingKeys.Logo)
            {
                throw new BadRequestException("Only the logo is changed by uploading an image.");
            }

            // Köhnə yol həmişə bazadan götürülür. Yeni şəkil əvvəl yüklənir ki, yanlış fayl olsa köhnə loqo itməsin
            var oldImage = setting.Value;
            var newImage = await _fileService.UploadFileAsync(model.Image!, Folder);
            setting.Value = newImage;

            try
            {
                await _settingRepo.UpdateAsync(setting);
            }
            catch
            {
                // Bazaya yazılmadısa yeni fayl yetim qalmasın
                await _fileService.DeleteFileAsync(newImage);
                throw;
            }

            // Yalnız bu sistemin yüklədiyi fayl silinir (başlanğıc /images/logo.png qalır)
            await _fileService.DeleteFileAsync(oldImage);
        }

        private static SettingDto ToDto(Setting setting)
        {
            return new SettingDto
            {
                Id = setting.Id,
                Key = setting.Key,
                Value = setting.Value
            };
        }
    }
}
