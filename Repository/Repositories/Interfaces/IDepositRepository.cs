using Domain.Entities;
using Domain.Enums;
using Repository.Results;

namespace Repository.Repositories.Interfaces
{
    public interface IDepositRepository
    {
        Task<List<DepositPlan>> GetPlansAsync(bool activeOnly);
        Task<DepositPlan?> GetPlanAsync(int id);
        Task UpdatePlanAsync(DepositPlan plan);

        // Bir tranzaksiyada: kartın balansı azalır, depozit və jurnal sətri yazılır. Ya hamısı olur, ya heç biri
        Task<DepositOpenResult> OpenAsync(DepositOpenPlan plan);

        Task<List<Deposit>> GetByUserAsync(string userId);
        Task<Deposit?> FindByReferenceAsync(string reference, string userId);

        // Admin siyahısı: axtarış (istifadəçi emaili, ad, kod), vəziyyət, səhifələmə
        Task<(List<Deposit> Items, int Total)> GetPagedAsync(string? search, DepositStatus? status, int page, int pageSize);
        Task<DepositTotals> GetTotalsAsync();

        // Müddəti bitmiş depozitlərin pulu və faizi karta qaytarılır. Ödənilənlərin siyahısını qaytarır
        Task<List<MaturedDeposit>> PayMaturedAsync(DateTime utcNow);
    }
}
