using Microsoft.AspNetCore.Mvc;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Client
{
    // Kart növlərinin qaydaları (haqq, cashback, limit, komissiya): ictimaidir, qiymətlər hamıya açıqdır
    [Route("api/card-tiers")]
    [ApiController]
    public class CardTiersController : ControllerBase
    {
        private readonly ICardTierService _service;

        public CardTiersController(ICardTierService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }
    }
}
