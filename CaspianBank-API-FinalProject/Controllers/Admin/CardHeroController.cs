using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.CardHeroes;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Home-un yuxarı hissəsindəki sol mətn. Tək yazıdır: yalnız oxunur və dəyişdirilir
    [Route("api/admin/card-hero")]
    [ApiController]
    [Authorize(Roles = "WebDesigner,SuperAdmin,Admin")]
    public class CardHeroController : ControllerBase
    {
        private readonly ICardHeroService _cardHeroService;
        public CardHeroController(ICardHeroService cardHeroService)
        {
            _cardHeroService = cardHeroService;
        }
        [HttpGet]
        public async Task<IActionResult> GetCardHero()
        {
            return Ok(await _cardHeroService.GetAsync());
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCardHero([FromRoute] int id, [FromBody] UpdateCardHeroDto request)
        {
            await _cardHeroService.UpdateAsync(id, request);
            return NoContent();
        }
    }
}
