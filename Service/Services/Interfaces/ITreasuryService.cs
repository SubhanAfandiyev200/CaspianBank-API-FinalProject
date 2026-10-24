using Service.Helpers.DTOs.Bank;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    // Bankın xəzinəsi və jurnalı (yalnız oxuma)
    public interface ITreasuryService
    {
        Task<ServiceResult<TreasuryPageDto>> GetPageAsync(TreasuryFilterDto filter);

        // Tamamlanmış ayı bağlayır: əməliyyat xərci və mənfəət vergisi xəzinədən çıxış kimi yazılır. Eyni ay ikinci dəfə bağlanmır
        Task<ServiceResult<TreasuryCloseDto>> CloseMonthAsync(string month);

        // Gündəlik iş: əvvəlki ay bağlanmayıbsa bağlayır (AutoCloseFromMonth-dan başlayaraq). Bağlanan ayların sayı
        Task<int> CloseDueMonthsAsync();
    }
}
