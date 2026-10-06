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
    }
}
