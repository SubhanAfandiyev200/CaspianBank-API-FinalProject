using Domain.Common;

namespace Domain.Entities
{
    public class ServiceSection : BaseEntity
    {
        public string Label { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ICollection<ServiceItem> ServiceItems { get; set; } = new List<ServiceItem>();
    }
}
