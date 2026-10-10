using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    // Ümumi əməliyyatlar (GetById, Add, Update, Delete) IBaseRepository-dədir; burada yalnız kart dizaynına məxsus sorğular qalır
    public interface ICardDesignRepository : IBaseRepository<CardDesign>
    {
        // Home üçün: göstərilənlər, sıra ilə, ən çoxu 'max' qədər (izlənmir, yalnız oxuma)
        Task<IEnumerable<CardDesign>> GetHomeAsync(int max);

        // Admin üçün: hamısı, sıra ilə
        Task<IEnumerable<CardDesign>> GetAllOrderedAsync();

        // Home-da göstərilən dizaynların sayı (exceptId verilibsə o dizayn sayılmır: onu redaktə edirik)
        Task<int> CountShownAsync(int? exceptId);

        // Dizaynı kartlar və ya kart növü qaydaları istifadə edirsə true (belə dizayn silinə bilməz)
        Task<bool> IsUsedAsync(int id);
    }
}
