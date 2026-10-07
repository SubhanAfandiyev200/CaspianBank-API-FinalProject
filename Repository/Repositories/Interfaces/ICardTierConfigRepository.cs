using Domain.Entities;
using Domain.Enums;

namespace Repository.Repositories.Interfaces
{
    public interface ICardTierConfigRepository : IBaseRepository<CardTierConfig>
    {
        Task<CardTierConfig?> GetByTierAsync(CardTier tier);
        Task<IReadOnlyList<CardTierConfig>> GetAllWithDesignAsync();
    }
}
