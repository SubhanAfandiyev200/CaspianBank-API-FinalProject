using Domain.Entities;
using Domain.Enums;

namespace Repository.Repositories.Interfaces
{
    // Dəstək mesajları (ticket). Oxuma metodları izləmir; GetWithMessagesAsync izləyir (mesaj əlavə edib SaveAsync ilə yazmaq üçün)
    public interface ISupportTicketRepository : IBaseRepository<SupportTicket>
    {
        // Müştərinin öz mesajları, ən yeni fəaliyyət əvvəldə (mesajları ilə, saymaq üçün)
        Task<IReadOnlyList<SupportTicket>> GetByUserAsync(string userId);

        // Müştərinin hələ bağlanmamış mesajlarının sayı (spam olmasın deyə limit)
        Task<int> CountOpenByUserAsync(string userId);

        // Müştərinin son vaxtlar yazdığı, hələ bağlanmamış "Help" mesajı (yeni mesaj həmin yazışmaya əlavə olunsun). İzləyir. Yoxdursa null
        Task<SupportTicket?> FindOpenHelpTicketAsync(string userId, DateTime since);

        // Qeydiyyatsız ziyarətçinin (email) hələ bağlanmamış mesajlarının sayı: spam limiti
        Task<int> CountOpenByGuestAsync(string email);

        // İzləyir. Mesajlar köhnədən yeniyə, müştərisi ilə. userId verilibsə yalnız onun mesajıdır (başqasınınkı null qayıdır)
        Task<SupportTicket?> GetWithMessagesAsync(int id, string? userId);

        // İşçinin siyahısı: vəziyyət, axtarış (mövzu, müştəri adı və emaili). Gözləyənlərin sayı da qayıdır
        Task<(IReadOnlyList<SupportTicket> Items, int Total, int PendingCount)> GetPageAsync(TicketStatus? status, string? search, int skip, int take);

        // İzlənən ticket-də edilən dəyişiklikləri (yeni mesaj, vəziyyət) bir yerdə yazır
        Task SaveAsync();
    }
}
