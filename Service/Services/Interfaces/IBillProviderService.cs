using Service.Helpers.DTOs.Bills;

namespace Service.Services.Interfaces
{
    // Admin: təchizatçının adı, loqosu və aktivliyi. Kod, kateqoriya və qaydalar dəyişmir. Xətalar global middleware-lə qaytarılır
    public interface IBillProviderService
    {
        Task<IEnumerable<BillProviderAdminDto>> GetAllAsync();
        Task<BillProviderAdminDto> GetDetailAsync(int id);
        Task UpdateAsync(int id, UpdateBillProviderDto model);
    }
}
