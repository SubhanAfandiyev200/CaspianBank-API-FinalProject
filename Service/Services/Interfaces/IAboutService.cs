using Service.Helpers.DTOs.Abouts;

namespace Service.Services.Interfaces
{
    // About blokunun mətni tək yazıdır: admin onu yalnız görür (GetAsync), mətnini (UpdateAsync) və videosunu (UpdateVideoAsync) dəyişir
    public interface IAboutService
    {
        Task<AboutDto?> GetUIAsync();
        Task<AboutDto> GetAsync();
        Task UpdateAsync(int id, AboutUpdateDto model);
        Task UpdateVideoAsync(int id, UpdateAboutVideoDto model);
    }
}
