using Service.Helpers.DTOs.Bills;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    // Müştərinin aylıq təkrarlanan ödənişləri. UserId HƏMİŞƏ tokendən götürülür, sorğudan yox
    public interface IRecurringPaymentService
    {
        Task<IEnumerable<RecurringPaymentDto>> GetAllAsync(string userId);

        // Nömrə/kod və məbləğ yoxlanılır. Eyni təchizatçı və nömrə üçün artıq varsa yenisi açılmır, mövcud olan yenilənir (məbləğ, kart)
        Task<ServiceResult<RecurringPaymentDto>> CreateAsync(string userId, CreateRecurringPaymentDto model);

        // Dayandırmaq və yenidən başlatmaq
        Task<ServiceResult<RecurringPaymentDto>> SetActiveAsync(string userId, int id, bool active);

        Task<ServiceResult<bool>> DeleteAsync(string userId, int id);

        // Gündəlik iş (arxa plan xidməti çağırır): bu ay işlənməyən aktiv ödənişləri edir. Uğurla ödənilənlərin sayını qaytarır
        Task<int> RunDueAsync();
    }
}
