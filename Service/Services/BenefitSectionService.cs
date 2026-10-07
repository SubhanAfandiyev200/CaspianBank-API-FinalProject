using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.BenefitSections;
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
    }
}
