namespace Service.Helpers.DTOs.AboutPillars
{
    public class AboutPillarDto
    {
        // Admin üçün lazımdır; Home-un ictimai cavabında da gəlir, MVC onu oxumur
        public int Id { get; set; }
        public string Image { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
