

using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.Responses.DTOs.HomeTickers;
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

        public async Task<IEnumerable<HomeTickerDto>> GetAllAsync()
        {
            var result = await _tickerRepo.GetAllAsync();
            return result.OrderBy(m => m.Id).Select(m => new HomeTickerDto
            {
                Text = m.Text
            });
        }
    }
}
