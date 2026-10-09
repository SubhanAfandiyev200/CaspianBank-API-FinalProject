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

        public async Task<BenefitItemDto> GetDetailAsync(int id)
        {
            var item = await _benefitItemRepo.GetByIdAsync(id);
            if (item is null) throw new NotFoundException();
            return ToDto(item);
        }

        public async Task CreateAsync(BenefitItemCreateDto model)
        {
            // Mətnlər, düymənin ünvanı və bölmə seçimi CreateBenefitItemDtoValidator-da yoxlanılır
            await _createValidator.EnsureValidAsync(model);
            await EnsureSectionExistsAsync(model.BenefitSectionId);

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
                BenefitSectionId = model.BenefitSectionId
            });
        }

        public async Task UpdateAsync(int id, BenefitItemUpdateDto model)
        {
            await _updateValidator.EnsureValidAsync(model);

            var item = await _benefitItemRepo.GetByIdAsync(id);
            if (item is null) throw new NotFoundException();

            await EnsureSectionExistsAsync(model.BenefitSectionId);

            // Modeldəki dəyərlər tapılan obyektə köçürülməsə, UpdateAsync köhnə dəyərləri yenidən yazar
            item.Label = model.Label!.Trim();
            item.Title = model.Title!.Trim();
            item.Description = model.Description!.Trim();
            item.ButtonText = model.ButtonText!.Trim();
            item.ButtonUrl = model.ButtonUrl!.Trim();
            item.Text1 = model.Text1!.Trim();
            item.Text2 = model.Text2!.Trim();
            item.Text3 = model.Text3!.Trim();
            item.BenefitSectionId = model.BenefitSectionId;
            await _benefitItemRepo.UpdateAsync(item);
        }

        public async Task DeleteAsync(int id)
        {
            var item = await _benefitItemRepo.GetByIdAsync(id);
            if (item is null) throw new NotFoundException();
            await _benefitItemRepo.DeleteAsync(item);
        }

        // Olmayan bölməyə bağlana bilməz: yoxsa baza xarici açar xətası (500) verərdi
        private async Task EnsureSectionExistsAsync(int sectionId)
        {
            var section = await _benefitSectionRepo.GetByIdAsync(sectionId);
            if (section is null)
            {
                throw new BadRequestException("Choose a section.");
            }
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
