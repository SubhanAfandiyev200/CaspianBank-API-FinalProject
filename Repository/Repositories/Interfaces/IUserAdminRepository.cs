using Domain.Entities;
using Repository.Results;

namespace Repository.Repositories.Interfaces
{
    // İşçilərin müştəriləri görməsi və dondurması. "Müştəri" = Customer rolunda olan istifadəçi (işçilər bura düşmür)
    public interface IUserAdminRepository
    {
        // search: ad, soyad, email, telefon və ya FİN. restricted: true = yalnız donmuşlar, false = yalnız aktivlər, null = hamısı
        Task<UserPageResult> GetCustomersPageAsync(string? search, bool? restricted, int skip, int take);

        Task<UserDetailData?> GetCustomerDetailAsync(string userId);

        // Dondurmaq/açmaq üçün (izləyir). Müştəri deyilsə və ya yoxdursa null
        Task<AppUser?> GetCustomerAsync(string userId);

        Task SaveAsync(AppUser user);
    }
}
