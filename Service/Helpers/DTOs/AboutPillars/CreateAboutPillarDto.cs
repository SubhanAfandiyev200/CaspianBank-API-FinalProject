using Microsoft.AspNetCore.Http;

namespace Service.Helpers.DTOs.AboutPillars
{
    public class CreateAboutPillarDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }

        // Boş ola bilər: bu halda validator "Choose an image." mesajını qaytarır
        public IFormFile? Image { get; set; }
    }
}
