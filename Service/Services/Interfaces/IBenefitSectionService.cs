using Service.Helpers.DTOs.BenefitSections;

namespace Service.Services.Interfaces
{
    // Benefits blokunun başlığı tək yazıdır: admin onu yalnız görür (GetAsync) və dəyişir (UpdateAsync)
    public interface IBenefitSectionService
    {
        Task<BenefitSectionDto?> GetUIAsync();
        Task<BenefitSectionDto> GetAsync();
        Task UpdateAsync(int id, BenefitSectionUpdateDto model);
    }
}
