using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Brands;
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
        public BrandService(IBrandRepository brandRepo)
        {
            _brandRepo = brandRepo;
        }

        public async Task<IEnumerable<BrandDto>> GetAllUIAsync()
        {
            var result = await _brandRepo.GetAllAsync();
            return result.OrderByDescending(m => m.CreatedAt).Select(m => new BrandDto
            {
                Image = m.Image,
                Name = m.Name
            });
        }
    }
}
