using Service.Helpers.DTOs.ServiceItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Interfaces
{
    public interface IServiceItemService
    {
        Task<IEnumerable<ServiceItemDto>> GetAllUIAsync();
    }
}
