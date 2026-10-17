using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Service.Helpers.DTOs.Bills;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Client
{
    // Müştərinin aylıq təkrarlanan ödənişləri (ayın 1-i). UserId HƏMİŞƏ tokendən götürülür, sorğudan yox
    [Route("api/bills/recurring")]
    [ApiController]
    [Authorize]
    public class RecurringPaymentsController : ControllerBase
    {
        private readonly IRecurringPaymentService _service;

        public RecurringPaymentsController(IRecurringPaymentService service)
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
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync(UserId));
        }

        // Eyni təchizatçı və nömrə üçün artıq varsa yenisi açılmır, mövcud olan yenilənir
        [EnableRateLimiting("transfer")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRecurringPaymentDto model)
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
            var result = await _service.DeleteAsync(UserId, id);
            if (result.IsNotFound)
            {
                return NotFound(new
                {
                    isSuccess = false,
                    errors = result.Errors
                });
            }
            return NoContent();
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
