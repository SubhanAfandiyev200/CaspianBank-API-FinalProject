using Service.Helpers.DTOs.CardDesigns;

namespace Service.Services.Interfaces
{
    public interface ICardDesignService
    {
        // Public: Home səhifəsi (yalnız göstərilənlər, sıra ilə, maksimum 3)
        Task<IEnumerable<CardDesignDto>> GetHomeAsync();

        // Admin
        Task<IEnumerable<CardDesignAdminDto>> GetAllAsync();
        Task<CardDesignAdminDto> GetDetailAsync(int id);
        Task CreateAsync(CreateCardDesignDto model);
        Task UpdateAsync(int id, UpdateCardDesignDto model);
        Task DeleteAsync(int id);
    }
}
