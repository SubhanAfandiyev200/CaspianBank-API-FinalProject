using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.HomeTickers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Interfaces
{
    public interface IHomeTickerService
    {
        Task<IEnumerable<HomeTickerDto>> GetAllUIAsync();
    }
}
