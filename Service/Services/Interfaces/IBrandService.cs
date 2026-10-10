using Service.Helpers.DTOs.Brands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Interfaces
{
    public interface IBrandService
    {
        Task<IEnumerable<BrandDto>> GetAllUIAsync();
        Task<IEnumerable<BrandDto>> GetAllAsync();
        Task<BrandDto> GetDetailAsync(int id);
        Task CreateAsync(CreateBrandDto model);
        Task DeleteAsync(int id);
        Task UpdateAsync(int id, UpdateBrandDto model);
    }
}
