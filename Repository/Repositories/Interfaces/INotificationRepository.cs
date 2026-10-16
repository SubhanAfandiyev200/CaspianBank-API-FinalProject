using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    // Bildirişlər. Ümumi əməliyyatlar (Add, GetById, Update) IBaseRepository-dədir; burada istifadəçiyə aid sorğular qalır
    public interface INotificationRepository : IBaseRepository<Notification>
    {
        // İstifadəçinin bildirişləri, yeni əvvəldə. unreadOnly: yalnız oxunmamışlar. Say süzgəcə görədir
        Task<(IReadOnlyList<Notification> Items, int Total)> GetPageAsync(string userId, bool unreadOnly, int skip, int take);

        Task<int> CountUnreadAsync(string userId);

        // Oxunmuş kimi işarələyir. Bildiriş bu istifadəçiyə aid deyilsə və ya yoxdursa false
        Task<bool> MarkReadAsync(int id, string userId);

        // Hamısını oxunmuş edir, dəyişən sətir sayını qaytarır
        Task<int> MarkAllReadAsync(string userId);

        // Eyni hadisə üçün bu istifadəçiyə bildiriş artıq yazılıb?
        Task<bool> ExistsAsync(string userId, string dedupeKey);

        // Yazır. Eyni DedupeKey ilə paralel yazma toqquşsa (unikal indeks) false qaytarır və sətri izləmədən çıxarır,
        // ki, eyni DbContext-dəki sonrakı yazmaları pozmasın
        Task<bool> TryAddAsync(Notification notification);
    }
}
