using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Helpers.DTOs.Brands
{
    public class UpdateBrandDto
    {
        public string? Name { get; set; }

        // İstəyə bağlıdır: boş olarsa köhnə şəkil qalır
        public IFormFile? Image { get; set; }
    }
}
