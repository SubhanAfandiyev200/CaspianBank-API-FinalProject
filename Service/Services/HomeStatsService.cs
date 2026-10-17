using Microsoft.Extensions.Options;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.HomeStats;
using Service.Helpers.Settings;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class HomeStatsService : IHomeStatsService
    {
        private readonly IHomeStatsRepository _statsRepo;
        private readonly HomeStatsSettings _settings;

        public HomeStatsService(IHomeStatsRepository statsRepo, IOptions<HomeStatsSettings> settings)
        {
            _statsRepo = statsRepo;
            _settings = settings.Value;
        }

        public async Task<HomeStatsDto> GetUIAsync()
        {
            // Rəqəmlər ardıcıl oxunur: eyni DbContext-də paralel sorğu icazəli deyil.
            // Hər rəqəm = real say + başlanğıc dəyər (HomeStatsSettings): bank Home-da daha böyük görünür, amma real əməliyyat rəqəmi artırır
            return new HomeStatsDto
            {
                CustomerCount = _settings.BaseCustomers + await _statsRepo.CountCustomersAsync(),
                TransferCount = _settings.BaseTransfers + await _statsRepo.CountTransfersAsync(),
                TransferVolume = _settings.BaseTransferVolume + await _statsRepo.SumTransferAmountAsync(),
                CardCount = _settings.BaseCards + await _statsRepo.CountCardsAsync()
            };
        }
    }
}
