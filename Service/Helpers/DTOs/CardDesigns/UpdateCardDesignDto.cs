using Microsoft.AspNetCore.Http;

namespace Service.Helpers.DTOs.CardDesigns
{
    public class UpdateCardDesignDto
    {
        public string? Title { get; set; }
        public bool ShowOnHome { get; set; }
        public int DisplayOrder { get; set; }

        // İstəyə bağlıdır: boş olarsa köhnə şəkil qalır
        public IFormFile? Image { get; set; }
    }
}
