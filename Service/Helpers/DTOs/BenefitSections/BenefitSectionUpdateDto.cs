using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Helpers.DTOs.BenefitSections
{
    public class BenefitSectionUpdateDto
    {
        public string? Label { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
    }
}
