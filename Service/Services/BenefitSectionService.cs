using Domain.Entities;
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
        private readonly IValidator<BenefitSectionCreateDto> _createValidator;
        private readonly IValidator<BenefitSectionUpdateDto> _updateValidator;
        public BenefitSectionService(IBenefitSectionRepository benefitSectionRepo,
                                     IValidator<BenefitSectionCreateDto> createValidator,
                                     IValidator<BenefitSectionUpdateDto> updateValidator)
        {
            _benefitSectionRepo = benefitSectionRepo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task CreateAsync(BenefitSectionCreateDto model)
        {
            // Label, title və description boş olmamalıdır və limiti keçməməlidir (CreateBenefitSectionDtoValidator)
            await _createValidator.EnsureValidAsync(model);

            await _benefitSectionRepo.AddAsync(new BenefitSection
            {
                Description = model.Description!.Trim(),
                Label = model.Label!.Trim(),
                Title = model.Title!.Trim()
            });
        }

        public async Task DeleteAsync(int id)
        {
            var benefitSection = await _benefitSectionRepo.GetByIdAsync(id);
            if (benefitSection is null) throw new NotFoundException();
            await _benefitSectionRepo.DeleteAsync(benefitSection);
        }

        public async Task<BenefitSectionDto> GetAsync()
        {
            var benefitSection = await _benefitSectionRepo.GetAsync();
            if (benefitSection is null) throw new NotFoundException();
            return new BenefitSectionDto
            {
                Title = benefitSection.Title,
                Label = benefitSection.Label,
                Description = benefitSection.Description,
                Id = benefitSection.Id
            };
        }

        // Admin siyahısı: bütün bölmələr, köhnədən yeniyə (id sırası ilə)
        public async Task<IEnumerable<BenefitSectionDto>> GetAllAsync()
        {
            var sections = await _benefitSectionRepo.GetAllAsync();
            return sections.OrderBy(m => m.Id).Select(m => new BenefitSectionDto
            {
                Id = m.Id,
                Label = m.Label,
                Title = m.Title,
                Description = m.Description
            });
        }

        public async Task<BenefitSectionDto> GetDetailAsync(int id)
        {
            var benefitSection = await _benefitSectionRepo.GetByIdAsync(id);
            if (benefitSection is null) throw new NotFoundException();
            return new BenefitSectionDto
            {
                Id = benefitSection.Id,
                Label = benefitSection.Label,
                Title = benefitSection.Title,
                Description = benefitSection.Description
            };
        }

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
