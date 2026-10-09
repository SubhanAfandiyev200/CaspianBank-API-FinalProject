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
        private const string VideoFolder = "about";

        private readonly IAboutRepository _aboutRepo;
        private readonly IFileService _fileService;
        private readonly IValidator<AboutUpdateDto> _updateValidator;
        private readonly IValidator<UpdateAboutVideoDto> _videoValidator;
        public AboutService(IAboutRepository aboutRepo,
                            IFileService fileService,
                            IValidator<AboutUpdateDto> updateValidator,
                            IValidator<UpdateAboutVideoDto> videoValidator)
        {
            _aboutRepo = aboutRepo;
            _fileService = fileService;
            _updateValidator = updateValidator;
            _videoValidator = videoValidator;
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

        public async Task UpdateVideoAsync(int id, UpdateAboutVideoDto model)
        {
            await _videoValidator.EnsureValidAsync(model);

            var about = await _aboutRepo.GetByIdAsync(id);
            if (about is null) throw new NotFoundException();

            // Köhnə yol həmişə bazadan götürülür. Yeni video əvvəl yüklənir ki, yanlış fayl olsa köhnə video itməsin
            var oldVideo = about.VideoPath;
            var newVideo = await _fileService.UploadVideoAsync(model.Video!, VideoFolder);
            about.VideoPath = newVideo;

            try
            {
                await _aboutRepo.UpdateAsync(about);
            }
            catch
            {
                // Bazaya yazılmadısa yeni fayl yetim qalmasın
                await _fileService.DeleteFileAsync(newVideo);
                throw;
            }

            // Yalnız bu sistemin yüklədiyi video silinir (başlanğıc /videos/homeVideo.mp4 qalır)
            await _fileService.DeleteFileAsync(oldVideo);
        }
    }
}
