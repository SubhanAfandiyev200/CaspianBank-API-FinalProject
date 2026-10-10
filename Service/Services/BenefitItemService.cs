using Domain.Constants;
using Domain.Entities;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.BenefitItems;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class BenefitItemService : IBenefitItemService
    {
        private readonly IBenefitItemRepository _benefitItemRepo;
        private readonly IBenefitSectionRepository _benefitSectionRepo;
        private readonly IValidator<BenefitItemCreateDto> _createValidator;
        private readonly IValidator<BenefitItemUpdateDto> _updateValidator;
        public BenefitItemService(IBenefitItemRepository benefitItemRepo,
                                  IBenefitSectionRepository benefitSectionRepo,
                                  IValidator<BenefitItemCreateDto> createValidator,
                                  IValidator<BenefitItemUpdateDto> updateValidator)
        {
            _benefitItemRepo = benefitItemRepo;
            _benefitSectionRepo = benefitSectionRepo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
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

        // Admin siyahısı: köhnədən yeniyə (id sırası ilə)
        public async Task<IEnumerable<BenefitItemDto>> GetAllAsync()
        {
            var items = await _benefitItemRepo.GetAllAsync();
            return items.OrderBy(m => m.Id).Select(ToDto);
        }

        public IEnumerable<ButtonDestinationDto> GetDestinations()
        {
            return ButtonDestinations.All.Select(d => new ButtonDestinationDto
            {
                Path = d.Path,
                Label = d.Label
            }).ToList();
        }

        public async Task<BenefitItemDto> GetDetailAsync(int id)
        {
            var item = await _benefitItemRepo.GetByIdAsync(id);
            if (item is null) throw new NotFoundException();
            return ToDto(item);
        }

        public async Task CreateAsync(BenefitItemCreateDto model)
        {
            // Mətnlər və düymənin ünvanı CreateBenefitItemDtoValidator-da yoxlanılır
            await _createValidator.EnsureValidAsync(model);

            // Bölmə tək olduğu üçün seçilmir: kart Home-da göstərilən hazırkı bölməyə bağlanır
            var section = await GetCurrentSectionAsync();

            await _benefitItemRepo.AddAsync(new BenefitItem
            {
                Label = model.Label!.Trim(),
                Title = model.Title!.Trim(),
                Description = model.Description!.Trim(),
                ButtonText = model.ButtonText!.Trim(),
                ButtonUrl = model.ButtonUrl!.Trim(),
                Text1 = model.Text1!.Trim(),
                Text2 = model.Text2!.Trim(),
                Text3 = model.Text3!.Trim(),
                BenefitSectionId = section.Id
            });
        }

        public async Task UpdateAsync(int id, BenefitItemUpdateDto model)
        {
            await _updateValidator.EnsureValidAsync(model);

            var item = await _benefitItemRepo.GetByIdAsync(id);
            if (item is null) throw new NotFoundException();

            // Modeldəki dəyərlər tapılan obyektə köçürülməsə, UpdateAsync köhnə dəyərləri yenidən yazar
            item.Label = model.Label!.Trim();
            item.Title = model.Title!.Trim();
            item.Description = model.Description!.Trim();
            item.ButtonText = model.ButtonText!.Trim();
            item.ButtonUrl = model.ButtonUrl!.Trim();
            item.Text1 = model.Text1!.Trim();
            item.Text2 = model.Text2!.Trim();
            item.Text3 = model.Text3!.Trim();
            await _benefitItemRepo.UpdateAsync(item);
        }

        public async Task DeleteAsync(int id)
        {
            var item = await _benefitItemRepo.GetByIdAsync(id);
            if (item is null) throw new NotFoundException();
            await _benefitItemRepo.DeleteAsync(item);
        }

        // Kart bölmə olmadan yaranmır (xarici açar xətası əvəzinə aydın mesaj verilir)
        private async Task<BenefitSection> GetCurrentSectionAsync()
        {
            var section = await _benefitSectionRepo.GetAsync();
            if (section is null)
            {
                throw new BadRequestException("There is no benefit section yet. Add one in the database first.");
            }
            return section;
        }

        private static BenefitItemDto ToDto(BenefitItem item)
        {
            return new BenefitItemDto
            {
                Id = item.Id,
                Label = item.Label,
                Title = item.Title,
                Description = item.Description,
                ButtonText = item.ButtonText,
                ButtonUrl = item.ButtonUrl,
                Text1 = item.Text1,
                Text2 = item.Text2,
                Text3 = item.Text3,
                BenefitSectionId = item.BenefitSectionId
            };
        }
    }
}
