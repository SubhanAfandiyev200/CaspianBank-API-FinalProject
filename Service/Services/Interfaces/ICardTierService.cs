using Service.Helpers.DTOs.Cards;

namespace Service.Services.Interfaces
{
    public interface ICardTierService
    {
        Task<IEnumerable<CardTierDto>> GetAllAsync();
    }
}
