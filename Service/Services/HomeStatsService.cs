using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.HomeStats;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class HomeStatsService : IHomeStatsService
    {
        private readonly IHomeStatsRepository _statsRepo;

        public HomeStatsService(IHomeStatsRepository statsRepo)
        {
            _statsRepo = statsRepo;
        }

        public async Task<HomeStatsDto> GetUIAsync()
        {
            // Rəqəmlər ardıcıl oxunur: eyni DbContext-də paralel sorğu icazəli deyil
            return new HomeStatsDto
            {
                CustomerCount = await _statsRepo.CountCustomersAsync(),
                TransferCount = await _statsRepo.CountTransfersAsync(),
                TransferVolume = await _statsRepo.SumTransferAmountAsync(),
                CardCount = await _statsRepo.CountCardsAsync()
            };
        }
    }
}
