using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Brands;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class BrandService : IBrandService
    {
        private readonly IBrandRepository _brandRepo;
        private readonly IFileService _fileService;
        public BrandService(IBrandRepository brandRepo,
                            IFileService fileService)
        {
            _brandRepo = brandRepo;
            _fileService = fileService;
        }

        public async Task<BrandDto> CreateAsync(CreateBrandDto model)
        {
            var name = model.Name?.Trim() ?? string.Empty;
            if (name.Length == 0)
            {
                throw new BadRequestException("Enter a name.");
            }
            if (name.Length > 100)
            {
                throw new BadRequestException("The name can be at most 100 characters.");
            }

            // Fayl /images/brands/<təsadüfi ad> altına yazılır (FileService yolun əvvəlinə /images/ özü əlavə edir)
            string imagePath = await _fileService.UploadFileAsync(model.Image!, "brands");

            var brand = new Brand
            {
                Image = imagePath,
                Name = name
            };

            try
            {
                await _brandRepo.AddAsync(brand);
            }
            catch
            {
                // Bazaya yazılmadısa şəkil faylı yetim qalmasın
                await _fileService.DeleteFileAsync(imagePath);
                throw;
            }

            return new BrandDto
            {
                Id = brand.Id,
                Image = brand.Image,
                Name = brand.Name
            };
        }

        public async Task<IEnumerable<BrandDto>> GetAllAsync()
        {
            var result = await _brandRepo.GetAllAsync();
            return result.OrderBy(m => m.CreatedAt).Select(m => new BrandDto
            {
                Id = m.Id,
                Image = m.Image,
                Name = m.Name
            });
        }

        public async Task<IEnumerable<BrandDto>> GetAllUIAsync()
        {
            var result = await _brandRepo.GetAllAsync();
            return result.OrderByDescending(m => m.CreatedAt).Select(m => new BrandDto
            {
                Id = m.Id,
                Image = m.Image,
                Name = m.Name
            });
        }

        public async Task<BrandDto> GetDetailAsync(int id)
        {
            var brand = await _brandRepo.GetByIdAsync(id);
            if (brand is null) throw new NotFoundException();
            return new BrandDto
            {
                Image = brand.Image,
                Id = brand.Id,
                Name = brand.Name
            };
        }

    }
}
