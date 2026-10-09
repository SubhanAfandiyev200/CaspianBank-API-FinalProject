using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Abouts;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class AboutService : IAboutService
    {
        private readonly IAboutRepository _aboutRepo;
        private readonly IValidator<AboutUpdateDto> _updateValidator;
        public AboutService(IAboutRepository aboutRepo,
                            IValidator<AboutUpdateDto> updateValidator)
        {
            _aboutRepo = aboutRepo;
            _updateValidator = updateValidator;
        }

        // Admin: Home-da göstərilən hazırkı mətn (Edit üçün Id ilə). Yoxdursa 404
        public async Task<AboutDto> GetAsync()
        {
            var about = await _aboutRepo.GetAsync();
            if (about is null) throw new NotFoundException();
            return new AboutDto
            {
                Id = about.Id,
                Label = about.Label,
                Title = about.Title,
                Description = about.Description,
                VideoPath = about.VideoPath
            };
        }

        // Home (ictimai): Id lazım deyil
        public async Task<AboutDto?> GetUIAsync()
        {
            var about = await _aboutRepo.GetAsync();
            if (about is null)
            {
                return null;
            }

            return new AboutDto
            {
                Label = about.Label,
                Title = about.Title,
                Description = about.Description,
                VideoPath = about.VideoPath
            };
        }

        public async Task UpdateAsync(int id, AboutUpdateDto model)
        {
            await _updateValidator.EnsureValidAsync(model);

            var about = await _aboutRepo.GetByIdAsync(id);
            if (about is null) throw new NotFoundException();

            // Modeldəki dəyərlər tapılan obyektə köçürülməsə, UpdateAsync köhnə dəyərləri yenidən yazar
            about.Label = model.Label!.Trim();
            about.Title = model.Title!.Trim();
            about.Description = model.Description!.Trim();
            await _aboutRepo.UpdateAsync(about);
        }
    }
}
