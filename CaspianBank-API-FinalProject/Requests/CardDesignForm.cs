namespace CaspianBank_API_FinalProject.Requests
{
    // Admin formasından (multipart/form-data) gələn kart dizaynı
    public class CardDesignForm
    {
        public string Title { get; set; } = string.Empty;
        public bool ShowOnHome { get; set; } = true;
        public int DisplayOrder { get; set; }
        public IFormFile? Image { get; set; }
    }
}
