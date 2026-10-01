using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.AboutPillars;
using Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            return pillars.OrderBy(m => m.Id).Select(m => new AboutPillarDto
            {
                Image = m.Image,
                Description = m.Description,
                Title = m.Title
            });
        }
    }
}
