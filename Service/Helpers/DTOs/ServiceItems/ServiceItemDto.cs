using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Helpers.DTOs.ServiceItems
{
    public class ServiceItemDto
    {
        public int Number { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ServiceSectionId { get; set; }
    }
}
