namespace Service.Helpers.DTOs.Abouts
{
    // Yalnız mətnlər dəyişir. Video faylı sabit qalır (böyük fayldır, serverdə əl ilə dəyişdirilir)
    public class AboutUpdateDto
    {
        public string? Label { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
    }
}
