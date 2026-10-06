namespace Service.Helpers.DTOs.CardDesigns
{
    // Admin üçün: gizli olanlar da daxil
    public class CardDesignAdminDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public bool ShowOnHome { get; set; }
        public int DisplayOrder { get; set; }
    }
}
