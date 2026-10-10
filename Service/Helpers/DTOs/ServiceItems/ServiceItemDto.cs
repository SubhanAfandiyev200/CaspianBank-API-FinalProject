namespace Service.Helpers.DTOs.ServiceItems
{
    public class ServiceItemDto
    {
        // Admin üçün lazımdır; Home-un ictimai cavabında da gəlir, MVC onu oxumur
        public int Id { get; set; }
        public int Number { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ServiceSectionId { get; set; }
    }
}
