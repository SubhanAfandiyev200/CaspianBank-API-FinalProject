using Microsoft.AspNetCore.Http;

namespace Service.Helpers.DTOs.CardDesigns
{
    public class CreateCardDesignDto
    {
        public string? Title { get; set; }
        public bool ShowOnHome { get; set; } = true;
        public int DisplayOrder { get; set; }

        // Boş ola bilər: bu halda validator "Choose a card image." mesajını qaytarır
        public IFormFile? Image { get; set; }
    }
}
