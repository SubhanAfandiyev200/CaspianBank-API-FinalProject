using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Cards;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class CardTierService : ICardTierService
    {
        private readonly ICardTierConfigRepository _repo;

        public CardTierService(ICardTierConfigRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<CardTierDto>> GetAllAsync()
        {
            var configs = await _repo.GetAllWithDesignAsync();
            return configs.Select(c => new CardTierDto
            {
                Tier = c.Tier.ToString(),
                IssueFee = c.IssueFee,
                CashbackPercent = c.CashbackPercent,
                TransferLimit = c.TransferLimit,
                CommissionPercent = c.CommissionPercent,
                DesignImage = c.CardDesign.Image
            }).ToList();
        }
    }
}
