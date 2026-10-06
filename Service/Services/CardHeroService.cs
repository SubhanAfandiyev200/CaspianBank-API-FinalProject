using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.CardHeroes;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class CardHeroService : ICardHeroService
    {
        private readonly ICardHeroRepository _heroRepo;

        public CardHeroService(ICardHeroRepository heroRepo)
        {
            _heroRepo = heroRepo;
        }

        public async Task<CardHeroDto?> GetUIAsync()
        {
            var hero = await _heroRepo.GetAsync();
            if (hero is null) return null;

            return new CardHeroDto
            {
                Label = hero.Label,
                Title = hero.Title,
                Description = hero.Description
            };
        }
    }
}
