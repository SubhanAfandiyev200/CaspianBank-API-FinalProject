using Service.Helpers.DTOs.CardHeroes;

namespace Service.Services.Interfaces
{
    // Hero mətni tək bir yazıdır: admin onu yalnız görür (GetAsync) və dəyişir (UpdateAsync), yeni yaratmaq və ya silmək lazım deyil
    public interface ICardHeroService
    {
        Task<CardHeroDto?> GetUIAsync();
        Task<CardHeroDto> GetAsync();
        Task UpdateAsync(int id, UpdateCardHeroDto model);
    }
}
