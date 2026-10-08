

using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.BenefitSections;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class BenefitSectionService : IBenefitSectionService
    {
        private readonly IBenefitSectionRepository _benefitSectionRepo;
        public BenefitSectionService(IBenefitSectionRepository benefitSectionRepo)
        {
            _benefitSectionRepo = benefitSectionRepo;
        }

        // Bazadakı limitlər (BenefitSectionConfiguration)
        private const int MaxLabelLength = 100;
        private const int MaxTitleLength = 200;
        private const int MaxDescriptionLength = 500;

        public async Task CreateAsync(BenefitSectionCreateDto model)
        {
            await _benefitSectionRepo.AddAsync(new BenefitSection
            {
                Description = Clean(model.Description, "description", MaxDescriptionLength),
                Label = Clean(model.Label, "label", MaxLabelLength),
                Title = Clean(model.Title, "title", MaxTitleLength)
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
            var benefitSection = await _benefitSectionRepo.GetByIdAsync(id);
            if (benefitSection is null) throw new NotFoundException();

            // Modeldəki dəyərlər tapılan obyektə köçürülməsə, UpdateAsync köhnə dəyərləri yenidən yazar
            benefitSection.Label = Clean(model.Label, "label", MaxLabelLength);
            benefitSection.Title = Clean(model.Title, "title", MaxTitleLength);
            benefitSection.Description = Clean(model.Description, "description", MaxDescriptionLength);
            await _benefitSectionRepo.UpdateAsync(benefitSection);
        }

        // Kənar boşluqlar silinir, boş və ya limitdən uzun mətn bazaya çatmadan 400 verir
        private static string Clean(string? value, string name, int maxLength)
        {
            var text = value?.Trim() ?? string.Empty;
            if (text.Length == 0)
            {
                throw new BadRequestException($"Enter the {name}.");
            }
            if (text.Length > maxLength)
            {
                throw new BadRequestException($"The {name} can be at most {maxLength} characters.");
            }
            return text;
        }
    }
}
