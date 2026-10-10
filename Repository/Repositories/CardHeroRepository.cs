using Domain.Entities;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories
{
    public class CardHeroRepository : BaseRepository<CardHero>, ICardHeroRepository
    {
        public CardHeroRepository(AppDbContext context) : base(context)
        {
        }
    }
}
