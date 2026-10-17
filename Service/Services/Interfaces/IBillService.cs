using Service.Helpers.DTOs.Bills;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    // Müştərinin ödənişləri. UserId HƏMİŞƏ tokendən götürülür, sorğudan yox
    public interface IBillService
    {
        // Müştəriyə göstərilən (aktiv) təchizatçılar
        Task<IEnumerable<BillProviderDto>> GetProvidersAsync();

        // Nömrə və ya abunəçi kodu ilə axtarış: kimin hesabıdır və nə qədər borcu var. Nömrə səhv operatora aiddirsə və ya yoxdursa aydın mesaj
        Task<ServiceResult<BillLookupDto>> LookupAsync(LookupBillDto model);

        // Ödəniş: pul kartdan çıxır, abunəçinin balansı artır. Eyni RequestId təkrar gələndə əvvəlki qəbz qaytarılır
        Task<ServiceResult<BillReceiptDto>> PayAsync(string userId, PayBillDto model);

        // Bu ay nə qədər ödənilib və son ödənişlər
        Task<BillsOverviewDto> GetOverviewAsync(string userId);

        // Gündəlik iş (arxa plan xidməti çağırır): bu ay hesabı yazılmamış abunəçilərə hesab yazır. Yazılanların sayını qaytarır
        Task<int> RunMonthlyBillingAsync();
    }
}
