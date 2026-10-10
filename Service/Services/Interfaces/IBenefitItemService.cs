using Service.Helpers.DTOs.BenefitItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Interfaces
{
    public interface IBenefitItemService
    {
        Task<IEnumerable<BenefitItemDto>> GetAllUIAsync();
        Task<IEnumerable<BenefitItemDto>> GetAllAsync();
        Task<BenefitItemDto> GetDetailAsync(int id);

        // Düymənin gedə biləcəyi hazır yerlər (admin linki yazmır, seçir)
        IEnumerable<ButtonDestinationDto> GetDestinations();
        Task CreateAsync(BenefitItemCreateDto model);
        Task DeleteAsync(int id);
        Task UpdateAsync(int id, BenefitItemUpdateDto model);
    }
}
