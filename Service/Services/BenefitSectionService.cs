using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.BenefitSections;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class BenefitSectionService : IBenefitSectionService
    {
        private readonly IBenefitSectionRepository _benefitSectionRepo;
        private readonly IValidator<BenefitSectionUpdateDto> _updateValidator;
        public BenefitSectionService(IBenefitSectionRepository benefitSectionRepo,
                                     IValidator<BenefitSectionUpdateDto> updateValidator)
        {
            _benefitSectionRepo = benefitSectionRepo;
            _updateValidator = updateValidator;
        }

        // Admin: Home-da göstərilən hazırkı başlıq (Edit üçün Id ilə). Yoxdursa 404
        public async Task<BenefitSectionDto> GetAsync()
        {
            var benefitSection = await _benefitSectionRepo.GetAsync();
            if (benefitSection is null) throw new NotFoundException();
            return new BenefitSectionDto
            {
                Id = benefitSection.Id,
                Label = benefitSection.Label,
                Title = benefitSection.Title,
                Description = benefitSection.Description
            };
        }

        // Home (ictimai): Id lazım deyil
        public async Task<BenefitSectionDto?> GetUIAsync()
        {
            var section = await _benefitSectionRepo.GetAsync();
            if (section is null)
            {
                return null;
            }

            return new BenefitSectionDto
            {
                Label = section.Label,
                Title = section.Title,
                Description = section.Description
            };
        }

        public async Task UpdateAsync(int id, BenefitSectionUpdateDto model)
        {
            await _updateValidator.EnsureValidAsync(model);

            var benefitSection = await _benefitSectionRepo.GetByIdAsync(id);
            if (benefitSection is null) throw new NotFoundException();

            // Modeldəki dəyərlər tapılan obyektə köçürülməsə, UpdateAsync köhnə dəyərləri yenidən yazar
            benefitSection.Label = model.Label!.Trim();
            benefitSection.Title = model.Title!.Trim();
            benefitSection.Description = model.Description!.Trim();
            await _benefitSectionRepo.UpdateAsync(benefitSection);
        }
    }
}
