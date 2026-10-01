using Microsoft.EntityFrameworkCore;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Abouts;
using Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class AboutService : IAboutService
    {
        private readonly IAboutRepository _aboutRepo;
        public AboutService(IAboutRepository aboutRepo)
        {
            _aboutRepo = aboutRepo;
        }

        public async Task<AboutDto?> GetUIAsync()
        {
            var about = await _aboutRepo.GetAsync();
            if (about is null) return null;

            return new AboutDto
            {
                Label = about.Label,
                Title = about.Title,
                Description = about.Description,
                VideoPath = about.VideoPath
            };
        }
    }
}
