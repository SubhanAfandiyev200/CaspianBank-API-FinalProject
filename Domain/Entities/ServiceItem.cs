using Domain.Common;

namespace Domain.Entities
{
    public class ServiceItem : BaseEntity
    {
        public int Number { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ServiceSectionId { get; set; }
        public ServiceSection ServiceSection { get; set; } = null!;
    }
}
