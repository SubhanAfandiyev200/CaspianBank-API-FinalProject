using Domain.Entities;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.ServiceItems;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class ServiceItemService : IServiceItemService
    {
        private const string Folder = "services";

        private readonly IServiceItemRepository _serviceItemRepo;
        private readonly IFileService _fileService;
        private readonly IValidator<ServiceItemUpdateDto> _updateValidator;
        public ServiceItemService(IServiceItemRepository serviceItemRepo,
                                  IFileService fileService,
                                  IValidator<ServiceItemUpdateDto> updateValidator)
        {
            _serviceItemRepo = serviceItemRepo;
            _fileService = fileService;
            _updateValidator = updateValidator;
        }

        public async Task<IEnumerable<ServiceItemDto>> GetAllUIAsync()
        {
            var items = await _serviceItemRepo.GetAllAsync();
            return items.OrderBy(m => m.Number).Select(ToDto);
        }

        // Admin siyahısı Home-dakı sıra ilə (Number)
        public async Task<IEnumerable<ServiceItemDto>> GetAllAsync()
        {
            var items = await _serviceItemRepo.GetAllAsync();
            return items.OrderBy(m => m.Number).Select(ToDto);
        }

        public async Task<ServiceItemDto> GetDetailAsync(int id)
        {
            var item = await _serviceItemRepo.GetByIdAsync(id);
            if (item is null) throw new NotFoundException();
            return ToDto(item);
        }

        public async Task UpdateAsync(int id, ServiceItemUpdateDto model)
        {
            await _updateValidator.EnsureValidAsync(model);

            var item = await _serviceItemRepo.GetByIdAsync(id);
            if (item is null) throw new NotFoundException();

            // Köhnə ikonun yolu həmişə bazadan götürülür. Yenisi əvvəl yüklənir ki, yanlış fayl olsa köhnə ikon itməsin
            var oldIcon = item.Icon;
            string? newIcon = null;
            if (model.Icon is not null && model.Icon.Length > 0)
            {
                newIcon = await _fileService.UploadFileAsync(model.Icon, Folder);
                item.Icon = newIcon;
            }

            // Modeldəki dəyərlər tapılan obyektə köçürülməsə, UpdateAsync köhnə dəyərləri yenidən yazar
            item.Title = model.Title!.Trim();
            item.Description = model.Description!.Trim();

            try
            {
                await _serviceItemRepo.UpdateAsync(item);
            }
            catch
            {
                // Bazaya yazılmadısa yeni fayl yetim qalmasın
                if (newIcon is not null)
                {
                    await _fileService.DeleteFileAsync(newIcon);
                }
                throw;
            }

            // Yeni ikon uğurla yazıldı: köhnəsi silinir (başlanğıc .svg ikonlar bu sistemin yüklədiyi fayl deyil, ona görə qalır)
            if (newIcon is not null)
            {
                await _fileService.DeleteFileAsync(oldIcon);
            }
        }

        private static ServiceItemDto ToDto(ServiceItem item)
        {
            return new ServiceItemDto
            {
                Id = item.Id,
                Number = item.Number,
                Icon = item.Icon,
                Title = item.Title,
                Description = item.Description,
                ServiceSectionId = item.ServiceSectionId
            };
        }
    }
}
