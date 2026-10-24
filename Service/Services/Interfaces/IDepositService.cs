using Service.Helpers.DTOs.Deposits;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    // Müddətli depozitlər. UserId HƏMİŞƏ tokendən götürülür. Müddət bitməmiş pulu çıxarmaq üçün heç bir əməliyyat yoxdur
    public interface IDepositService
    {
        // Müştərinin variantları, depozitləri və cəmləri
        Task<DepositsOverviewDto> GetOverviewAsync(string userId);

        // Depozit aç: pul kartdan çıxır. Eyni RequestId təkrar gələndə əvvəlki depozit qaytarılır (pul bir dəfə çıxır)
        Task<ServiceResult<DepositDto>> OpenAsync(string userId, OpenDepositDto model);

        // Admin: bütün depozitlər, cəmlər və variantlar
        Task<ServiceResult<DepositPageDto>> GetPageAsync(DepositFilterDto filter);

        // Admin: variantın faizini, hədlərini və aktivliyini dəyişir (müddət dəyişmir, açılmış depozitlərə təsir etmir)
        Task<ServiceResult<DepositPlanDto>> UpdatePlanAsync(int id, UpdateDepositPlanDto model);

        // Gündəlik iş (arxa plan xidməti çağırır): müddəti bitənlərin pulu və faizi karta qaytarılır. Ödənilənlərin sayı
        Task<int> RunMaturityAsync();
    }
}
