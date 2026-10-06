using Service.Helpers.DTOs.CardHeroes;

namespace Service.Services.Interfaces
{
    public interface ICardHeroService
    {
        Task<CardHeroDto?> GetUIAsync();
    }
}
