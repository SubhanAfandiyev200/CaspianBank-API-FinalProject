using Microsoft.AspNetCore.Http;

namespace Service.Helpers.DTOs.AboutPillars
{
    public class UpdateAboutPillarDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }

        // İstəyə bağlıdır: boş olarsa köhnə şəkil qalır
        public IFormFile? Image { get; set; }
    }
}
