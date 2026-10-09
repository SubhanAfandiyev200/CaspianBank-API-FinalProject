using Service.Helpers.DTOs.ServiceSections;

namespace Service.Services.Interfaces
{
    // Services blokunun başlığı tək yazıdır: admin onu yalnız görür (GetAsync) və dəyişir (UpdateAsync)
    public interface IServiceSectionService
    {
        Task<ServiceSectionDto?> GetUIAsync();
        Task<ServiceSectionDto> GetAsync();
        Task UpdateAsync(int id, ServiceSectionUpdateDto model);
    }
}
