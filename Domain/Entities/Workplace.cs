using Domain.Common;

namespace Domain.Entities
{
    // İş yeri (şirkət, təşkilat). Vətəndaşla əlaqə Employment cədvəli ilə (many-to-many)
    public class Workplace : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;       // VÖEN, 10 rəqəm
        public string Sector { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public ICollection<Employment> Employments { get; set; } = new List<Employment>();
    }
}
