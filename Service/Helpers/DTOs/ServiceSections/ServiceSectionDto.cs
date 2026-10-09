namespace Service.Helpers.DTOs.ServiceSections
{
    public class ServiceSectionDto
    {
        // Admin üçün lazımdır (Edit); Home-un ictimai cavabında 0 gəlir və istifadə olunmur
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
