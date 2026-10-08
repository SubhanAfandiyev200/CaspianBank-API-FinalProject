using Service.Helpers.DTOs.BenefitSections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Interfaces
{
    public interface IBenefitSectionService
    {
        Task<BenefitSectionDto?> GetUIAsync();
        Task<BenefitSectionDto> GetAsync();
        Task<IEnumerable<BenefitSectionDto>> GetAllAsync();
        Task<BenefitSectionDto> GetDetailAsync(int id);
        Task CreateAsync(BenefitSectionCreateDto model);
        Task DeleteAsync(int id);
        Task UpdateAsync(int id, BenefitSectionUpdateDto model);
    }
}
