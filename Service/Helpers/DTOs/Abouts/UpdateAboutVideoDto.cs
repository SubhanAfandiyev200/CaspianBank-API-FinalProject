using Microsoft.AspNetCore.Http;

namespace Service.Helpers.DTOs.Abouts
{
    public class UpdateAboutVideoDto
    {
        // Boş ola bilər: bu halda validator "Choose a video." mesajını qaytarır
        public IFormFile? Video { get; set; }
    }
}
