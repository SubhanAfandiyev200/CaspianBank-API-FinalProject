using Service.Helpers.DTOs.CardTiers;
using Service.Helpers.DTOs.Cards;

namespace Service.Services.Interfaces
{
    // Dörd kart növü (Cashback, Standard, Silver, Gold) kodda sabitdir, ona görə admin yalnız qiymətləri və dizaynı dəyişir (əlavə/silmə yoxdur)
    public interface ICardTierService
    {
        // İctimai: add-card səhifəsi qiymətləri buradan göstərir
        Task<IEnumerable<CardTierDto>> GetAllAsync();

        // Admin
        Task<IEnumerable<CardTierAdminDto>> GetAllAdminAsync();
        Task<CardTierAdminDto> GetDetailAsync(int id);
        Task UpdateAsync(int id, CardTierUpdateDto model);
    }
}
