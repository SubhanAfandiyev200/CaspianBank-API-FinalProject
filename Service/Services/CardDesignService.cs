using Domain.Entities;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.CardDesigns;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class CardDesignService : ICardDesignService
    {
        // Home səhifəsindəki kart yelpazəsi (animasiya) maksimum 3 karta görə qurulub
        private const int MaxHomeCards = 3;
        private const string Folder = "cards";

        private readonly ICardDesignRepository _repo;
        private readonly IFileService _files;
        private readonly IValidator<CreateCardDesignDto> _createValidator;
        private readonly IValidator<UpdateCardDesignDto> _updateValidator;

        public CardDesignService(ICardDesignRepository repo,
                                 IFileService files,
                                 IValidator<CreateCardDesignDto> createValidator,
                                 IValidator<UpdateCardDesignDto> updateValidator)
        {
            _repo = repo;
            _files = files;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<IEnumerable<CardDesignDto>> GetHomeAsync()
        {
            var designs = await _repo.GetHomeAsync(MaxHomeCards);
            return designs.Select(d => new CardDesignDto { Title = d.Title, Image = d.Image }).ToList();
        }

        public async Task<IEnumerable<CardDesignAdminDto>> GetAllAsync()
        {
            var designs = await _repo.GetAllOrderedAsync();
            return designs.Select(ToAdminDto).ToList();
        }

        public async Task<CardDesignAdminDto> GetDetailAsync(int id)
        {
            var design = await _repo.GetByIdAsync(id);
            if (design is null) throw new NotFoundException();
            return ToAdminDto(design);
        }

        public async Task CreateAsync(CreateCardDesignDto model)
        {
            // Ad, sıra və şəklin seçilməsi CreateCardDesignDtoValidator-da yoxlanılır
            await _createValidator.EnsureValidAsync(model);
            await EnsureHomeSlotAsync(model.ShowOnHome, null);

            // Şəklin ölçüsünü və növünü FileService yoxlayır, yanlışdırsa global exception middleware 400 qaytarır
            var imagePath = await _files.UploadFileAsync(model.Image!, Folder);

            try
            {
                await _repo.AddAsync(new CardDesign
                {
                    Title = model.Title!.Trim(),
                    Image = imagePath,
                    ShowOnHome = model.ShowOnHome,
                    DisplayOrder = model.DisplayOrder
                });
            }
            catch
            {
                // Bazaya yazılmadısa şəkil faylı yetim qalmasın
                await _files.DeleteFileAsync(imagePath);
                throw;
            }
        }

        public async Task UpdateAsync(int id, UpdateCardDesignDto model)
        {
            await _updateValidator.EnsureValidAsync(model);

            var design = await _repo.GetByIdAsync(id);
            if (design is null) throw new NotFoundException();

            await EnsureHomeSlotAsync(model.ShowOnHome, id);

            // Dəyərlər tapılan obyektə köçürülməsə, UpdateAsync köhnə dəyərləri yenidən yazar
            design.Title = model.Title!.Trim();
            design.ShowOnHome = model.ShowOnHome;
            design.DisplayOrder = model.DisplayOrder;

            // Köhnə şəklin yolu həmişə bazadan götürülür. Yenisi əvvəl yüklənir ki, yanlış fayl olsa köhnə şəkil itməsin
            var oldImage = design.Image;
            string? newImage = null;
            if (model.Image is not null && model.Image.Length > 0)
            {
                newImage = await _files.UploadFileAsync(model.Image, Folder);
                design.Image = newImage;
            }

            try
            {
                await _repo.UpdateAsync(design);
            }
            catch
            {
                // Bazaya yazılmadısa yeni fayl yetim qalmasın
                if (newImage is not null)
                {
                    await _files.DeleteFileAsync(newImage);
                }
                throw;
            }

            // Yeni şəkil uğurla yazıldı: köhnəsi silinir
            if (newImage is not null)
            {
                await _files.DeleteFileAsync(oldImage);
            }
        }

        public async Task DeleteAsync(int id)
        {
            var design = await _repo.GetByIdAsync(id);
            if (design is null) throw new NotFoundException();

            // Kartlar və ya kart növü qaydaları bu dizayna bağlıdırsa silinmir (bazada da Restrict qoyulub)
            if (await _repo.IsUsedAsync(id))
            {
                throw new BadRequestException("This design is used by a card type or by customer cards, so it cannot be deleted. Hide it from Home instead.");
            }

            var image = design.Image;

            // Əvvəl baza, sonra fayl: baza silməsi xəta versə şəkil yerində qalır
            await _repo.DeleteAsync(design);
            await _files.DeleteFileAsync(image);
        }

        // Home-da artıq 3 dizayn göstərilirsə dördüncünü göstərmək olmaz (yelpazədə yer yoxdur və sakitcə görünməzdi)
        private async Task EnsureHomeSlotAsync(bool showOnHome, int? exceptId)
        {
            if (!showOnHome)
            {
                return;
            }

            var shown = await _repo.CountShownAsync(exceptId);
            if (shown >= MaxHomeCards)
            {
                throw new BadRequestException($"Only {MaxHomeCards} designs can be shown on Home. Hide another one first.");
            }
        }

        private static CardDesignAdminDto ToAdminDto(CardDesign d)
        {
            return new CardDesignAdminDto
            {
                Id = d.Id,
                Title = d.Title,
                Image = d.Image,
                ShowOnHome = d.ShowOnHome,
                DisplayOrder = d.DisplayOrder
            };
        }
    }
}
