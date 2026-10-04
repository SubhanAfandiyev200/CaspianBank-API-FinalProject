using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.BenefitItems;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class BenefitItemService : IBenefitItemService
    {
        private readonly IBenefitItemRepository _benefitItemRepo;
        public BenefitItemService(IBenefitItemRepository benefitItemRepo)
        {
            _benefitItemRepo = benefitItemRepo;
        }

        public async Task<IEnumerable<BenefitItemDto>> GetAllUIAsync()
        {
            var items = await _benefitItemRepo.GetAllAsync();
            return items.OrderByDescending(m => m.CreatedAt).Select(m => new BenefitItemDto
            {
                Label = m.Label,
                Title = m.Title,
                Description = m.Description,
                ButtonText = m.ButtonText,
                ButtonUrl = m.ButtonUrl,
                Text1 = m.Text1,
                Text2 = m.Text2,
                Text3 = m.Text3,
                BenefitSectionId = m.BenefitSectionId
            });
        }
    }
}
