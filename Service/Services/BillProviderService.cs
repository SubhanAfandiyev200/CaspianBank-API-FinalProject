using Domain.Entities;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers;
using Service.Helpers.DTOs.Bills;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class BillProviderService : IBillProviderService
    {
        private const string Folder = "providers";

        private readonly IBillRepository _bills;
        private readonly IBaseRepository<BillProvider> _providers;
        private readonly IFileService _files;
        private readonly IValidator<UpdateBillProviderDto> _updateValidator;

        public BillProviderService(IBillRepository bills,
                                   IBaseRepository<BillProvider> providers,
                                   IFileService files,
                                   IValidator<UpdateBillProviderDto> updateValidator)
        {
            _bills = bills;
            _providers = providers;
            _files = files;
            _updateValidator = updateValidator;
        }

        public async Task<IEnumerable<BillProviderAdminDto>> GetAllAsync()
        {
            var providers = await _bills.GetProvidersAsync(false);
            return providers.Select(BillMapper.ToAdminDto).ToList();
        }

        public async Task<BillProviderAdminDto> GetDetailAsync(int id)
        {
            var provider = await _bills.GetProviderAsync(id);
            if (provider is null)
            {
                throw new NotFoundException();
            }
            return BillMapper.ToAdminDto(provider);
        }

        public async Task UpdateAsync(int id, UpdateBillProviderDto model)
        {
            await _updateValidator.EnsureValidAsync(model);

            var provider = await _providers.GetByIdAsync(id);
            if (provider is null)
            {
                throw new NotFoundException();
            }

            provider.Name = model.Name!.Trim();
            provider.IsActive = model.IsActive;

            // Köhnə loqonun yolu həmişə bazadan götürülür. Yenisi əvvəl yüklənir ki, yanlış fayl olsa köhnə loqo itməsin
            var oldLogo = provider.LogoUrl;
            string? newLogo = null;
            if (model.Logo is not null && model.Logo.Length > 0)
            {
                newLogo = await _files.UploadFileAsync(model.Logo, Folder);
                provider.LogoUrl = newLogo;
            }

            try
            {
                await _providers.UpdateAsync(provider);
            }
            catch
            {
                // Bazaya yazılmadısa yeni fayl yetim qalmasın
                if (newLogo is not null)
                {
                    await _files.DeleteFileAsync(newLogo);
                }
                throw;
            }

            // Yeni loqo uğurla yazıldı: köhnəsi silinir (başlanğıc loqolar sistemin yüklədiyi fayl deyil, onlar silinmir)
            if (newLogo is not null && !string.IsNullOrEmpty(oldLogo))
            {
                await _files.DeleteFileAsync(oldLogo);
            }
        }
    }
}
