using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.CardTiers;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Kart növlərinin qaydaları (açılış haqqı, cashback, limit, komissiya, dizayn). Dörd növ kodda sabitdir: yalnız oxunur və dəyişdirilir.
    // Pul qaydalarını Accountant, Admin və SuperAdmin dəyişə bilər
    [Route("api/admin/card-tiers")]
    [ApiController]
    [Authorize(Roles = Roles.Accountant + "," + Roles.Admin + "," + Roles.SuperAdmin)]
    public class CardTierController : ControllerBase
    {
        private readonly ICardTierService _cardTierService;
        private readonly ICardDesignService _cardDesignService;
        public CardTierController(ICardTierService cardTierService, ICardDesignService cardDesignService)
        {
            _cardTierService = cardTierService;
            _cardDesignService = cardDesignService;
        }
        // Edit formasındakı dizayn siyahısı. Kart dizaynlarının öz endpoint-i (api/admin/card-designs) yalnız Admin və SuperAdmin üçündür, Accountant da oxuya bilsin deyə burada ayrıca verilir
        [HttpGet("designs")]
        public async Task<IActionResult> GetDesigns()
        {
            return Ok(await _cardDesignService.GetAllAsync());
        }
        [HttpGet]
        public async Task<IActionResult> GetAllCardTiers()
        {
            return Ok(await _cardTierService.GetAllAdminAsync());
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCardTierDetail([FromRoute] int id)
        {
            return Ok(await _cardTierService.GetDetailAsync(id));
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCardTier([FromRoute] int id, [FromBody] CardTierUpdateDto request)
        {
            await _cardTierService.UpdateAsync(id, request);
            return NoContent();
        }
    }
}
