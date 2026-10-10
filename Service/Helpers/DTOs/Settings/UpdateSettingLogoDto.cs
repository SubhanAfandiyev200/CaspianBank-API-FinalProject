using Microsoft.AspNetCore.Http;

namespace Service.Helpers.DTOs.Settings
{
    public class UpdateSettingLogoDto
    {
        // Boş ola bilər: bu halda validator "Choose an image." mesajını qaytarır
        public IFormFile? Image { get; set; }
    }
}
