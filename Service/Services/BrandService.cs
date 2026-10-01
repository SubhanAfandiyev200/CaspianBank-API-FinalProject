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

        public async Task<IEnumerable<BrandUIVM>> GetAllUIAsync()
        {
            var result = await _brandRepo.GetAllAsync();
            return result.OrderBy(m => m.Id).Select(m => new BrandUIVM
            {
                Image = m.Image,
                Name = m.Name
            });
        }
    }
}
