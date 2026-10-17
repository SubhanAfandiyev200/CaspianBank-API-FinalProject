using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Service.Helpers.DTOs.Support;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Client
{
    // Müştərinin dəstək mesajları: yazır, cavabı oxuyur, davam edir, bağlayır. UserId HƏMİŞƏ tokendən götürülür
    [Route("api/support/tickets")]
    [ApiController]
    [Authorize]
    public class SupportController : ControllerBase
    {
        private readonly ISupportTicketService _service;

        public SupportController(ISupportTicketService service)
        {
            _service = service;
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
            return Ok(await _service.GetMineAsync(UserId));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            return ToResponse(await _service.GetMineDetailAsync(UserId, id));
        }

        [EnableRateLimiting("transfer")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTicketDto model)
        {
            return ToResponse(await _service.CreateAsync(UserId, model));
        }

        [EnableRateLimiting("transfer")]
        [HttpPost("{id:int}/messages")]
        public async Task<IActionResult> Reply(int id, [FromBody] ReplyTicketDto model)
        {
            return ToResponse(await _service.CustomerReplyAsync(UserId, id, model));
        }

        [HttpPost("{id:int}/close")]
        public async Task<IActionResult> Close(int id)
        {
            return ToResponse(await _service.CustomerCloseAsync(UserId, id));
        }

        private IActionResult ToResponse<T>(ServiceResult<T> result)
        {
            if (result.IsNotFound)
            {
                return NotFound(new { isSuccess = false, errors = result.Errors });
            }
            if (!result.IsSuccess)
            {
                return BadRequest(new { isSuccess = false, errors = result.Errors });
            }
            return Ok(result.Data);
        }
    }
}
