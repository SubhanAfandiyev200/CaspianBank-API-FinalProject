using Service.Helpers.DTOs.AboutPillars;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Interfaces
{
    public interface IAboutPillarService
    {
        Task<IEnumerable<AboutPillarDto>> GetAllUIAsync();
    }
}
