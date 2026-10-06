using Service.Helpers.DTOs.ServiceSections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Interfaces
{
    public interface IServiceSectionService
    {
        Task<ServiceSectionDto?> GetUIAsync();
    }
}
