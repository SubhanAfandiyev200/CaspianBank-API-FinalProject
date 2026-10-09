using Domain.Entities;
using FluentValidation;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.HomeTickers;
using Service.Helpers.Exceptions;
using Service.Helpers.Validators;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class HomeTickerService : IHomeTickerService
    {
        private readonly IHomeTickerRepository _tickerRepo;
        private readonly IValidator<CreateHomeTickerDto> _createValidator;
        private readonly IValidator<UpdateHomeTickerDto> _updateValidator;
        public HomeTickerService(IHomeTickerRepository tickerRepo,
                                 IValidator<CreateHomeTickerDto> createValidator,
                                 IValidator<UpdateHomeTickerDto> updateValidator)
        {
            _tickerRepo = tickerRepo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
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
            return new HomeTickerDto
            {
                Text = ticker.Text,
                Id = ticker.Id
            };
        }

        public async Task CreateAsync(CreateHomeTickerDto model)
        {
            // Mətnin boş olmaması və uzunluğu CreateHomeTickerDtoValidator-da yoxlanılır
            await _createValidator.EnsureValidAsync(model);
            await _tickerRepo.AddAsync(new HomeTicker { Text = model.Text!.Trim() });
        }

        public async Task UpdateAsync(int id, UpdateHomeTickerDto model)
        {
            await _updateValidator.EnsureValidAsync(model);

            var ticker = await _tickerRepo.GetByIdAsync(id);
            if (ticker is null) throw new NotFoundException();

            ticker.Text = model.Text!.Trim();
            await _tickerRepo.UpdateAsync(ticker);
        }

        public async Task DeleteAsync(int id)
        {
            var ticker = await _tickerRepo.GetByIdAsync(id);
            if (ticker is null) throw new NotFoundException();
            await _tickerRepo.DeleteAsync(ticker);
        }
    }
}
