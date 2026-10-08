using Service.Helpers.DTOs.HomeTickers;

namespace Service.Services.Interfaces
{
    public interface IHomeTickerService
    {
        Task<IEnumerable<HomeTickerDto>> GetAllUIAsync();
        Task<IEnumerable<HomeTickerDto>> GetAllAsync();
        Task<HomeTickerDto> GetDetailAsync(int id);
        Task<HomeTickerDto> CreateAsync(CreateHomeTickerDto model);
        Task<HomeTickerDto> UpdateAsync(int id, UpdateHomeTickerDto model);
        Task DeleteAsync(int id);
    }
}
