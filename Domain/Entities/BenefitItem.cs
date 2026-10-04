using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class BenefitItem : BaseEntity
    {
        public string Label { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ButtonText { get; set; }
        public string ButtonUrl { get; set; }
        public string Text1 { get; set; }
        public string Text2 { get; set; }
        public string Text3 { get; set; }
        public BenefitSection BenefitSection { get; set; }
        public int BenefitSectionId { get; set; }
    }
}
