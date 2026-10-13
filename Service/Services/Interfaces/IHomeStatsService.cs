using Service.Helpers.DTOs.HomeStats;

namespace Service.Services.Interfaces
{
    public interface IHomeStatsService
    {
        Task<HomeStatsDto> GetUIAsync();
    }
}
