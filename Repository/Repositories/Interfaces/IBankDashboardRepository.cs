using Repository.Results;

namespace Repository.Repositories.Interfaces
{
    // Bank dashboard üçün oxuma sorğuları (hamısı izləmir, heç nə yazmır)
    public interface IBankDashboardRepository
    {
        // [fromUtc, toUtc) aralığında gəlir və cashback xərci
        Task<MonthMoney> GetMonthMoneyAsync(DateTime fromUtc, DateTime toUtc);

        // Müştəri sayı, depozitlər, kreditlər, gözləyən işlər. Qeydiyyat sayı [monthFromUtc, monthToUtc) aralığı üçündür
        Task<BankSnapshot> GetSnapshotAsync(DateTime monthFromUtc, DateTime monthToUtc);
    }
}
