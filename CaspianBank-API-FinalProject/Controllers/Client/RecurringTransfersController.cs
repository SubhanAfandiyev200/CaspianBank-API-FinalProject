using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Service.Helpers.DTOs.Transfers;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Client
{
    // Aylıq təkrarlanan köçürmələr. UserId HƏMİŞƏ tokendən götürülür, sorğudan yox
    [Route("api/recurring-transfers")]
    [ApiController]
    [Authorize]
    public class RecurringTransfersController : ControllerBase
    {
        private readonly IRecurringTransferService _service;

        public RecurringTransfersController(IRecurringTransferService service)
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
        public async Task<IActionResult> Get()
        {
            return Ok(await _service.GetAsync(UserId));
        }

        // Alıcının kartı yoxlanır (nömrə təxmin edilməsin deyə sürət limiti var)
        [EnableRateLimiting("transfer")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRecurringTransferDto model)
        {
            return ToResponse(await _service.CreateAsync(UserId, model));
        }

        [HttpPost("{id:int}/pause")]
        public async Task<IActionResult> Pause(int id)
        {
            return ToResponse(await _service.SetActiveAsync(UserId, id, false));
        }

        [HttpPost("{id:int}/resume")]
        public async Task<IActionResult> Resume(int id)
        {
            return ToResponse(await _service.SetActiveAsync(UserId, id, true));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            return ToResponse(await _service.DeleteAsync(UserId, id));
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
