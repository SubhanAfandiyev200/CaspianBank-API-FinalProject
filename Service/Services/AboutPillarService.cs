using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.AboutPillars;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class AboutPillarService : IAboutPillarService
    {
        private readonly IAboutPillarRepository _pillarRepo;
        public AboutPillarService(IAboutPillarRepository pillarRepo)
        {
            _pillarRepo = pillarRepo;
        }

        public async Task<IEnumerable<AboutPillarDto>> GetAllUIAsync()
        {
            var pillars = await _pillarRepo.GetAllAsync();
            return pillars.OrderByDescending(m => m.CreatedAt).Select(m => new AboutPillarDto
            {
                Image = m.Image,
                Description = m.Description,
                Title = m.Title
            });
        }
    }
}
