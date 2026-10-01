using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Brand : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
    }
}
