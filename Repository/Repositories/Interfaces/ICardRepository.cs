using Domain.Entities;
using Repository.Results;

namespace Repository.Repositories.Interfaces
{
    public interface ICardRepository : IBaseRepository<Card>
    {
        // Yalnız həmin istifadəçinin kartları (dizaynı ilə): GetAllAsync() hamının kartını qaytarır, istifadəçiyə açılmamalıdır
        Task<IReadOnlyList<Card>> GetByUserAsync(string userId);

        // Kart yalnız sahibinə aiddirsə tapılır (başqasının kartına baxmağın qarşısını alır). Dəyişdirmək üçün izlənir.
        Task<Card?> GetByIdForUserAsync(int cardId, string userId);

        // Balansı artırır və "TopUp" qeydini yazır (bir SaveChanges). false: eyni anda başqa əməliyyat balansı dəyişib (RowVersion)
        Task<bool> TopUpAsync(Card card, decimal amount, string description);

        Task<Card?> GetByNumberAsync(string cardNumber);
        Task<bool> CardNumberExistsAsync(string cardNumber);
        Task<bool> UserHasCardsAsync(string userId);

        // Bir neçə kartı bir dəfəyə (atomik) yazır
        // false: yazıla bilmədi (məs. istifadəçinin Cashback kartı artıq var: unikal index)
        Task<bool> AddRangeAsync(IEnumerable<Card> cards);

        // Haqqı seçilən kartın balansından çıxarır və yeni kartı yazır: hamısı bir tranzaksiyada
        Task<CardOpenResult> AddPaidCardAsync(Card newCard, int fundingCardId, string userId, decimal fee);
    }
}
