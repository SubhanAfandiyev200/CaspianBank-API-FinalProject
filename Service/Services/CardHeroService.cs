using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.CardHeroes;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class CardHeroService : ICardHeroService
    {
        private readonly ICardHeroRepository _heroRepo;
        private readonly IValidator<UpdateCardHeroDto> _updateValidator;

        public CardHeroService(ICardHeroRepository heroRepo,
                               IValidator<UpdateCardHeroDto> updateValidator)
        {
            _heroRepo = heroRepo;
            _updateValidator = updateValidator;
        }

        // Admin: Home-da göstərilən hazırkı hero mətni (Edit üçün Id ilə). Yoxdursa 404
        public async Task<CardHeroDto> GetAsync()
        {
            var hero = await _heroRepo.GetAsync();
            if (hero is null) throw new NotFoundException();
            return new CardHeroDto
            {
                Id = hero.Id,
                Label = hero.Label,
                Title = hero.Title,
                Description = hero.Description
            };
        }

        // Home (ictimai): Id lazım deyil
        public async Task<CardHeroDto?> GetUIAsync()
        {
            var hero = await _heroRepo.GetAsync();
            if (hero is null)
            {
                return null;
            }

            return new CardHeroDto
            {
                Label = hero.Label,
                Title = hero.Title,
                Description = hero.Description
            };
        }

        public async Task UpdateAsync(int id, UpdateCardHeroDto model)
        {
            await _updateValidator.EnsureValidAsync(model);

            var hero = await _heroRepo.GetByIdAsync(id);
            if (hero is null) throw new NotFoundException();

            // Modeldəki dəyərlər tapılan obyektə köçürülməsə, UpdateAsync köhnə dəyərləri yenidən yazar
            hero.Label = model.Label!.Trim();
            hero.Title = model.Title!.Trim();
            hero.Description = model.Description!.Trim();
            await _heroRepo.UpdateAsync(hero);
        }
    }
}
