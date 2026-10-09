using Service.Helpers.DTOs.Abouts;

namespace Service.Services.Interfaces
{
    // About blokunun mətni tək yazıdır: admin onu yalnız görür (GetAsync) və dəyişir (UpdateAsync)
    public interface IAboutService
    {
        Task<AboutDto?> GetUIAsync();
        Task<AboutDto> GetAsync();
        Task UpdateAsync(int id, AboutUpdateDto model);
    }
}
