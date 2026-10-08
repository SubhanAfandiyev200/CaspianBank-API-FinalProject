using Domain.Entities;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.CardDesigns;
using Service.Helpers.Responses;
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

        public async Task<ServiceResult<CardDesignAdminDto>> CreateAsync(CreateCardDesignDto model)
        {
            var validation = await _createValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<CardDesignAdminDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var imagePath = await _files.UploadFileAsync(model.Image!, Folder);

            var design = new CardDesign
            {
                Title = model.Title.Trim(),
                Image = imagePath,
                ShowOnHome = model.ShowOnHome,
                DisplayOrder = model.DisplayOrder
            };

            try
            {
                await _repo.AddAsync(design);
            }
            catch
            {
                // Bazaya yazılmadısa şəkil faylı yetim qalmasın
                await _files.DeleteFileAsync(imagePath);
                throw;
            }

            return ServiceResult<CardDesignAdminDto>.Ok(ToAdminDto(design));
        }

        public async Task<ServiceResult<CardDesignAdminDto>> UpdateAsync(int id, UpdateCardDesignDto model)
        {
            var validation = await _updateValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<CardDesignAdminDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var design = await _repo.GetByIdAsync(id);
            if (design is null)
            {
                return ServiceResult<CardDesignAdminDto>.NotFound();
            }

            design.Title = model.Title.Trim();
            design.ShowOnHome = model.ShowOnHome;
            design.DisplayOrder = model.DisplayOrder;

            string? oldImage = null;
            string? newImage = null;
            if (model.Image is not null && model.Image.Length > 0)
            {
                newImage = await _files.UploadFileAsync(model.Image, Folder);
                oldImage = design.Image;
                design.Image = newImage;
            }

            try
            {
                await _repo.UpdateAsync(design);
            }
            catch
            {
                if (newImage is not null)
                {
                    await _files.DeleteFileAsync(newImage);
                }
                throw;
            }

            // Yeni şəkil uğurla yazıldı: köhnəsi silinir
            if (oldImage is not null)
            {
                await _files.DeleteFileAsync(oldImage);
            }

            return ServiceResult<CardDesignAdminDto>.Ok(ToAdminDto(design));
        }

        public async Task<OperationResponse> DeleteAsync(int id)
        {
            var design = await _repo.GetByIdAsync(id);
            if (design is null)
            {
                return new OperationResponse { IsSuccess = false, Errors = new[] { "Not found." } };
            }

            var image = design.Image;
            await _repo.DeleteAsync(design);
            await _files.DeleteFileAsync(image);

            return new OperationResponse { IsSuccess = true };
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
