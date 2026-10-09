using Service.Helpers.DTOs.AboutPillars;

namespace Service.Services.Interfaces
{
    public interface IAboutPillarService
    {
        Task<IEnumerable<AboutPillarDto>> GetAllUIAsync();
        Task<IEnumerable<AboutPillarDto>> GetAllAsync();
        Task<AboutPillarDto> GetDetailAsync(int id);
        Task CreateAsync(CreateAboutPillarDto model);
        Task UpdateAsync(int id, UpdateAboutPillarDto model);
        Task DeleteAsync(int id);
    }
}
