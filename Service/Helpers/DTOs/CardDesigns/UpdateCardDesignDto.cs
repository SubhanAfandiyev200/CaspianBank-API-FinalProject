namespace Service.Helpers.DTOs.CardDesigns
{
    public class UpdateCardDesignDto
    {
        public string Title { get; set; } = string.Empty;
        public bool ShowOnHome { get; set; }
        public int DisplayOrder { get; set; }

        // Boş olarsa köhnə şəkil qalır
        public UploadedImage? Image { get; set; }
    }
}
