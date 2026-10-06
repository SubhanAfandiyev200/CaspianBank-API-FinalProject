using Service.Helpers.DTOs.CardDesigns;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    public interface ICardDesignService
    {
        // Public: Home səhifəsi (yalnız göstərilənlər, sıra ilə, maksimum 3)
        Task<IEnumerable<CardDesignDto>> GetHomeAsync();

        // Admin
        Task<IEnumerable<CardDesignAdminDto>> GetAllAsync();
        Task<ServiceResult<CardDesignAdminDto>> CreateAsync(CreateCardDesignDto model);
        Task<ServiceResult<CardDesignAdminDto>> UpdateAsync(int id, UpdateCardDesignDto model);
        Task<OperationResponse> DeleteAsync(int id);
    }
}
