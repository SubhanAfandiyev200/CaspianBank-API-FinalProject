

using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.ServiceItems;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class ServiceItemService : IServiceItemService
    {
        private readonly IServiceItemRepository _serviceItemRepo;
        public ServiceItemService(IServiceItemRepository serviceItemRepo)
        {
            _serviceItemRepo = serviceItemRepo;
        }

        public async Task<IEnumerable<ServiceItemDto>> GetAllUIAsync()
        {
            var items = await _serviceItemRepo.GetAllAsync();
            return items.OrderBy(m => m.Number).Select(m => new ServiceItemDto
            {
                Number = m.Number,
                Icon = m.Icon,
                Title = m.Title,
                Description = m.Description,
                ServiceSectionId = m.ServiceSectionId
            });
        }
    }
}
