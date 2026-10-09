using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.ServiceSections;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class ServiceSectionService : IServiceSectionService
    {
        private readonly IServiceSectionRepository _serviceSectionRepo;
        private readonly IValidator<ServiceSectionUpdateDto> _updateValidator;
        public ServiceSectionService(IServiceSectionRepository serviceSectionRepo,
                                     IValidator<ServiceSectionUpdateDto> updateValidator)
        {
            _serviceSectionRepo = serviceSectionRepo;
            _updateValidator = updateValidator;
        }

        // Admin: Home-da göstərilən hazırkı başlıq (Edit üçün Id ilə). Yoxdursa 404
        public async Task<ServiceSectionDto> GetAsync()
        {
            var section = await _serviceSectionRepo.GetAsync();
            if (section is null) throw new NotFoundException();
            return new ServiceSectionDto
            {
                Id = section.Id,
                Label = section.Label,
                Title = section.Title,
                Description = section.Description
            };
        }

        // Home (ictimai): Id lazım deyil
        public async Task<ServiceSectionDto?> GetUIAsync()
        {
            var section = await _serviceSectionRepo.GetAsync();
            if (section is null)
            {
                return null;
            }

            return new ServiceSectionDto
            {
                Label = section.Label,
                Title = section.Title,
                Description = section.Description
            };
        }

        public async Task UpdateAsync(int id, ServiceSectionUpdateDto model)
        {
            await _updateValidator.EnsureValidAsync(model);

            var section = await _serviceSectionRepo.GetByIdAsync(id);
            if (section is null) throw new NotFoundException();

            // Modeldəki dəyərlər tapılan obyektə köçürülməsə, UpdateAsync köhnə dəyərləri yenidən yazar
            section.Label = model.Label!.Trim();
            section.Title = model.Title!.Trim();
            section.Description = model.Description!.Trim();
            await _serviceSectionRepo.UpdateAsync(section);
        }
    }
}
