using Domain.Entities;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.AboutPillars;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class AboutPillarService : IAboutPillarService
    {
        private const string Folder = "pillars";

        private readonly IAboutPillarRepository _pillarRepo;
        private readonly IAboutRepository _aboutRepo;
        private readonly IFileService _fileService;
        private readonly IValidator<CreateAboutPillarDto> _createValidator;
        private readonly IValidator<UpdateAboutPillarDto> _updateValidator;
        public AboutPillarService(IAboutPillarRepository pillarRepo,
                                  IAboutRepository aboutRepo,
                                  IFileService fileService,
                                  IValidator<CreateAboutPillarDto> createValidator,
                                  IValidator<UpdateAboutPillarDto> updateValidator)
        {
            _pillarRepo = pillarRepo;
            _aboutRepo = aboutRepo;
            _fileService = fileService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<IEnumerable<AboutPillarDto>> GetAllUIAsync()
        {
            var pillars = await _pillarRepo.GetAllAsync();
            return pillars.OrderByDescending(m => m.CreatedAt).Select(ToDto);
        }

        // Admin siyahısı: köhnədən yeniyə (id sırası ilə)
        public async Task<IEnumerable<AboutPillarDto>> GetAllAsync()
        {
            var pillars = await _pillarRepo.GetAllAsync();
            return pillars.OrderBy(m => m.Id).Select(ToDto);
        }

        public async Task<AboutPillarDto> GetDetailAsync(int id)
        {
            var pillar = await _pillarRepo.GetByIdAsync(id);
            if (pillar is null) throw new NotFoundException();
            return ToDto(pillar);
        }

        public async Task CreateAsync(CreateAboutPillarDto model)
        {
            // Ad, açıqlama və şəklin seçilməsi CreateAboutPillarDtoValidator-da yoxlanılır
            await _createValidator.EnsureValidAsync(model);

            // About tək olduğu üçün seçilmir: sütun Home-da göstərilən hazırkı About-a bağlanır
            var about = await _aboutRepo.GetAsync();
            if (about is null)
            {
                throw new BadRequestException("There is no About block yet. Add one in the database first.");
            }

            // Fayl /images/pillars/<təsadüfi ad> altına yazılır
            var imagePath = await _fileService.UploadFileAsync(model.Image!, Folder);

            try
            {
                await _pillarRepo.AddAsync(new AboutPillar
                {
                    Title = model.Title!.Trim(),
                    Description = model.Description!.Trim(),
                    Image = imagePath,
                    AboutId = about.Id
                });
            }
            catch
            {
                // Bazaya yazılmadısa şəkil faylı yetim qalmasın
                await _fileService.DeleteFileAsync(imagePath);
                throw;
            }
        }

        public async Task UpdateAsync(int id, UpdateAboutPillarDto model)
        {
            await _updateValidator.EnsureValidAsync(model);

            var pillar = await _pillarRepo.GetByIdAsync(id);
            if (pillar is null) throw new NotFoundException();

            // Köhnə şəklin yolu həmişə bazadan götürülür. Yenisi əvvəl yüklənir ki, yanlış fayl olsa köhnə şəkil itməsin
            var oldImage = pillar.Image;
            string? newImage = null;
            if (model.Image is not null && model.Image.Length > 0)
            {
                newImage = await _fileService.UploadFileAsync(model.Image, Folder);
                pillar.Image = newImage;
            }

            // Modeldəki dəyərlər tapılan obyektə köçürülməsə, UpdateAsync köhnə dəyərləri yenidən yazar
            pillar.Title = model.Title!.Trim();
            pillar.Description = model.Description!.Trim();

            try
            {
                await _pillarRepo.UpdateAsync(pillar);
            }
            catch
            {
                // Bazaya yazılmadısa yeni fayl yetim qalmasın
                if (newImage is not null)
                {
                    await _fileService.DeleteFileAsync(newImage);
                }
                throw;
            }

            // Yeni şəkil uğurla yazıldı: köhnəsi silinir (yalnız bu sistemin yüklədiyi fayl silinir)
            if (newImage is not null)
            {
                await _fileService.DeleteFileAsync(oldImage);
            }
        }

        public async Task DeleteAsync(int id)
        {
            var pillar = await _pillarRepo.GetByIdAsync(id);
            if (pillar is null) throw new NotFoundException();

            var image = pillar.Image;

            // Əvvəl baza, sonra fayl: baza silməsi xəta versə şəkil yerində qalır
            await _pillarRepo.DeleteAsync(pillar);
            await _fileService.DeleteFileAsync(image);
        }

        private static AboutPillarDto ToDto(AboutPillar pillar)
        {
            return new AboutPillarDto
            {
                Id = pillar.Id,
                Image = pillar.Image,
                Title = pillar.Title,
                Description = pillar.Description
            };
        }
    }
}
