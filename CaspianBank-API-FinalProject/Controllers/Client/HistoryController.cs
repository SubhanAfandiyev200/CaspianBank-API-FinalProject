using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.History;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Client
{
    // Giriş etmiş müştərinin əməliyyat tarixçəsi və kart çıxarışı. UserId HƏMİŞƏ tokendən götürülür, sorğudan yox
    [Route("api/history")]
    [ApiController]
    [Authorize]
    public class HistoryController : ControllerBase
    {
        private readonly IHistoryService _historyService;

        public HistoryController(IHistoryService historyService)
        {
            _historyService = historyService;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        // Bütün kartların əməliyyatları: filtr (kart, növ, istiqamət, tarix, axtarış) və səhifələmə ilə
        [HttpGet]
        public async Task<IActionResult> GetHistory([FromQuery] HistoryFilterDto filter)
        {
            return Ok(await _historyService.GetHistoryAsync(UserId, filter));
        }

        // Bir kartın seçilmiş tarix aralığı üçün çıxarışı
        [HttpGet("statement")]
        public async Task<IActionResult> GetStatement([FromQuery] StatementFilterDto filter)
        {
            return Ok(await _historyService.GetStatementAsync(UserId, filter));
        }
    }
}
