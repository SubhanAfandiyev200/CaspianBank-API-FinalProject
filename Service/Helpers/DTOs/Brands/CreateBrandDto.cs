using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Helpers.DTOs.Brands
{
    public class CreateBrandDto
    {
        public string? Name { get; set; }

        // Boş ola bilər: bu halda ASP.NET-in ümumi xətası əvəzinə FileService-in "Choose an image." mesajı qayıdır
        public IFormFile? Image { get; set; }
    }
}
