using Service.Helpers.DTOs.Cards;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    public interface ICardService
    {
        Task<IEnumerable<CardDto>> GetMyCardsAsync(string userId);
        Task<ServiceResult<CardDetailDto>> GetMyCardAsync(string userId, int cardId);

        // İlk giriş: Standard + Cashback pulsuz (balans 0, pul top-up ilə əlavə olunur)
        Task<ServiceResult<IEnumerable<CardDto>>> CreateInitialCardsAsync(string userId, CreateInitialCardsDto model);

        // Əlavə kart: haqq seçilən kartın balansından çıxılır
        Task<ServiceResult<CardDto>> AddCardAsync(string userId, AddCardDto model);

        // Balansı artırır (simulyasiya olunmuş xarici kartdan). Cashback kartına əlavə edilmir
        Task<ServiceResult<CardDto>> TopUpAsync(string userId, int cardId, TopUpDto model);

        // Kartın ən son əməliyyatları (yalnız sahibi görür)
        Task<ServiceResult<IEnumerable<TransactionDto>>> GetTransactionsAsync(string userId, int cardId, int take);

        // Bütün kartların son əməliyyatları (Cards səhifəsindəki "Recent activity")
        Task<IEnumerable<TransactionDto>> GetRecentActivityAsync(string userId, int take);

        // Kartı bloklamaq / blokdan çıxarmaq: əvvəl hesabın emailinə 6 rəqəmli kod göndərilir (Send...CodeAsync),
        // sonra istifadəçi kodu yazır (BlockAsync / UnblockAsync)
        Task<ServiceResult<CardCodeSentDto>> SendBlockCodeAsync(string userId, int cardId);
        Task<ServiceResult<CardDto>> BlockAsync(string userId, int cardId, BlockCardDto model);
        Task<ServiceResult<CardCodeSentDto>> SendUnblockCodeAsync(string userId, int cardId);
        Task<ServiceResult<CardDto>> UnblockAsync(string userId, int cardId, UnblockCardDto model);
    }
}
