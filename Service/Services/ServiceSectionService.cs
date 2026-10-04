using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.ServiceSections;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class ServiceSectionService : IServiceSectionService
    {
        private readonly IServiceSectionRepository _serviceSectionRepo;
        public ServiceSectionService(IServiceSectionRepository serviceSectionRepo)
        {
            _serviceSectionRepo = serviceSectionRepo;
        }

        public async Task<ServiceSectionDto?> GetUIAsync()
        {
            var section = await _serviceSectionRepo.GetAsync();
            if (section is null) return null;

            return new ServiceSectionDto
            {
                Label = section.Label,
                Title = section.Title,
                Description = section.Description
            };
        }
    }
}
