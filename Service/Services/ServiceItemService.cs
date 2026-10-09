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
        private readonly IServiceItemRepository _serviceItemRepo;
        private readonly IValidator<ServiceItemUpdateDto> _updateValidator;
        public ServiceItemService(IServiceItemRepository serviceItemRepo,
                                  IValidator<ServiceItemUpdateDto> updateValidator)
        {
            _serviceItemRepo = serviceItemRepo;
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

            // Modeldəki dəyərlər tapılan obyektə köçürülməsə, UpdateAsync köhnə dəyərləri yenidən yazar
            item.Title = model.Title!.Trim();
            item.Description = model.Description!.Trim();
            await _serviceItemRepo.UpdateAsync(item);
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
