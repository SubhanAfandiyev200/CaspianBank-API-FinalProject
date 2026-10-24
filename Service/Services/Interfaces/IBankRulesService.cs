using Service.Helpers.DTOs.Bank;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    // Bankın qaydaları (OwnBank sətri): maliyyə və kredit. Dəyişikliklər audit-ə yazılır
    public interface IBankRulesService
    {
        Task<ServiceResult<BankRulesDto>> GetAsync();

        // Yeniləyir və dəyişənlərin qısa izahını ("Reserve 10% → 12%") qaytarır (audit üçün)
        Task<ServiceResult<(BankRulesDto Rules, string Changes)>> UpdateAsync(BankRulesDto model);
    }
}
