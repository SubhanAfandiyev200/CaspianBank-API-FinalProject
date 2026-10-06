using Domain.Common;

namespace Domain.Entities
{
    public class AboutPillar : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public int AboutId { get; set; }
        public About About { get; set; } = null!;
    }
}
