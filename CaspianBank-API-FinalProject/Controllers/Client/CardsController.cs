using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Service.Helpers.DTOs.Cards;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Client
{
    // Giriş etmiş müştərinin kartları. UserId HƏMİŞƏ tokendən götürülür, sorğudan yox:
    // beləliklə heç kim başqasının kartına baxa və ya onu dəyişə bilməz.
    [Route("api/cards")]
    [ApiController]
    [Authorize]
    public class CardsController : ControllerBase
    {
        private readonly ICardService _cardService;

        public CardsController(ICardService cardService)
        {
            _cardService = cardService;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            return Ok(await _cardService.GetMyCardsAsync(UserId));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOne(int id)
        {
            return ToResponse(await _cardService.GetMyCardAsync(UserId, id));
        }

        // Bütün kartların ən son əməliyyatları (Cards səhifəsindəki "Recent activity"). "activity" sabit sözdür, {id:int} ilə qarışmır
        [HttpGet("activity")]
        public async Task<IActionResult> GetActivity([FromQuery] int take = 8)
        {
            return Ok(await _cardService.GetRecentActivityAsync(UserId, take));
        }

        // Kartın ən son əməliyyatları (kart səhifəsindəki "Recent activity")
        [HttpGet("{id:int}/transactions")]
        public async Task<IActionResult> GetTransactions(int id, [FromQuery] int take = 20)
        {
            return ToResponse(await _cardService.GetTransactionsAsync(UserId, id, take));
        }

        // İlk kartlar: Standard + Cashback (pulsuz)
        [HttpPost("initial")]
        public async Task<IActionResult> CreateInitial([FromBody] CreateInitialCardsDto model)
        {
            return ToResponse(await _cardService.CreateInitialCardsAsync(UserId, model));
        }

        // Əlavə kart (haqq seçilən kartdan çıxılır)
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddCardDto model)
        {
            return ToResponse(await _cardService.AddCardAsync(UserId, model));
        }

        // Balansı artırır (xarici kart simulyasiya olunur)
        [HttpPost("{id:int}/top-up")]
        public async Task<IActionResult> TopUp(int id, [FromBody] TopUpDto model)
        {
            return ToResponse(await _cardService.TopUpAsync(UserId, id, model));
        }

        // Bloklama / blokdan çıxarma iki addımdır: əvvəl kod emailə göndərilir (.../code), sonra kodla təsdiq olunur.
        // Email göndərən və kodu yoxlayan endpoint-lər giriş endpoint-ləri kimi sürət limitlidir
        [EnableRateLimiting("auth")]
        [HttpPost("{id:int}/block/code")]
        public async Task<IActionResult> SendBlockCode(int id)
        {
            return ToResponse(await _cardService.SendBlockCodeAsync(UserId, id));
        }

        [EnableRateLimiting("auth")]
        [HttpPost("{id:int}/block")]
        public async Task<IActionResult> Block(int id, [FromBody] BlockCardDto model)
        {
            return ToResponse(await _cardService.BlockAsync(UserId, id, model));
        }

        [EnableRateLimiting("auth")]
        [HttpPost("{id:int}/unblock/code")]
        public async Task<IActionResult> SendUnblockCode(int id)
        {
            return ToResponse(await _cardService.SendUnblockCodeAsync(UserId, id));
        }

        [EnableRateLimiting("auth")]
        [HttpPost("{id:int}/unblock")]
        public async Task<IActionResult> Unblock(int id, [FromBody] UnblockCardDto model)
        {
            return ToResponse(await _cardService.UnblockAsync(UserId, id, model));
        }

        private IActionResult ToResponse<T>(ServiceResult<T> result)
        {
            if (result.IsNotFound)
            {
                return NotFound(new
                {
                    isSuccess = false,
                    errors = result.Errors
                });
            }
            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    isSuccess = false,
                    errors = result.Errors
                });
            }
            return Ok(result.Data);
        }
    }
}
