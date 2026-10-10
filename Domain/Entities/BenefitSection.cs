using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class BenefitSection : BaseEntity
    {
        public string Label { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ICollection<BenefitItem> BenefitItems { get; set; } = new List<BenefitItem>();
    }
}
