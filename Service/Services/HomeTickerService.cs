

using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.HomeTickers;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class HomeTickerService : IHomeTickerService
    {
        private readonly IHomeTickerRepository _tickerRepo;
        public HomeTickerService(IHomeTickerRepository tickerRepo)
        {
            _tickerRepo = tickerRepo;
        }

        public async Task<IEnumerable<HomeTickerDto>> GetAllUIAsync()
        {
            var result = await _tickerRepo.GetAllAsync();
            return result.OrderByDescending(m => m.CreatedAt).Select(m => new HomeTickerDto
            {
                Text = m.Text
            });
        }
    }
}
