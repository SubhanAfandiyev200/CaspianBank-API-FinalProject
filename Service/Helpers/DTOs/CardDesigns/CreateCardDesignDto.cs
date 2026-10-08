using Microsoft.AspNetCore.Http;

namespace Service.Helpers.DTOs.CardDesigns
{
    public class CreateCardDesignDto
    {
        public string Title { get; set; } = string.Empty;
        public bool ShowOnHome { get; set; } = true;
        public int DisplayOrder { get; set; }
        public IFormFile? Image { get; set; }
    }
}
