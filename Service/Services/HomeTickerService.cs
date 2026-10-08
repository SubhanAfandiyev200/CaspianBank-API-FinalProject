using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.HomeTickers;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class HomeTickerService : IHomeTickerService
    {
        // Bazadakı HomeTickers.Text sütununun uzunluğu (HomeTickerConfiguration)
        private const int MaxTextLength = 100;

        private readonly IHomeTickerRepository _tickerRepo;
        public HomeTickerService(IHomeTickerRepository tickerRepo)
        {
            _tickerRepo = tickerRepo;
        }

        // Admin siyahısı: köhnədən yeniyə (id sırası ilə)
        public async Task<IEnumerable<HomeTickerDto>> GetAllAsync()
        {
            var result = await _tickerRepo.GetAllAsync();
            return result.OrderBy(m => m.Id).Select(m => new HomeTickerDto
            {
                Id = m.Id,
                Text = m.Text
            });
        }

        public async Task<IEnumerable<HomeTickerDto>> GetAllUIAsync()
        {
            var result = await _tickerRepo.GetAllAsync();
            return result.OrderByDescending(m => m.CreatedAt).Select(m => new HomeTickerDto
            {
                Id = m.Id,
                Text = m.Text
            });
        }

        public async Task<HomeTickerDto> GetDetailAsync(int id)
        {
            var ticker = await _tickerRepo.GetByIdAsync(id);
            if (ticker is null) throw new NotFoundException();
            return ToDto(ticker);
        }

        public async Task<HomeTickerDto> CreateAsync(CreateHomeTickerDto model)
        {
            var ticker = new HomeTicker { Text = CleanText(model.Text) };
            await _tickerRepo.AddAsync(ticker);
            return ToDto(ticker);
        }

        public async Task<HomeTickerDto> UpdateAsync(int id, UpdateHomeTickerDto model)
        {
            var ticker = await _tickerRepo.GetByIdAsync(id);
            if (ticker is null) throw new NotFoundException();

            ticker.Text = CleanText(model.Text);
            await _tickerRepo.UpdateAsync(ticker);
            return ToDto(ticker);
        }

        public async Task DeleteAsync(int id)
        {
            var ticker = await _tickerRepo.GetByIdAsync(id);
            if (ticker is null) throw new NotFoundException();
            await _tickerRepo.DeleteAsync(ticker);
        }

        // Boşluqlar kənarlardan silinir (Home-da mətn Trim ilə göstərilir, aralarındakı məsafəni CSS verir)
        private static string CleanText(string? text)
        {
            var value = text?.Trim() ?? string.Empty;
            if (value.Length == 0)
            {
                throw new BadRequestException("Enter the text.");
            }
            if (value.Length > MaxTextLength)
            {
                throw new BadRequestException($"The text can be at most {MaxTextLength} characters.");
            }
            return value;
        }

        private static HomeTickerDto ToDto(HomeTicker ticker)
        {
            return new HomeTickerDto
            {
                Id = ticker.Id,
                Text = ticker.Text
            };
        }
    }
}
