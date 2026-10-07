using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Service.Helpers.DTOs.Transfers;
using Service.Helpers.Responses;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Client
{
    // Köçürmələr. UserId HƏMİŞƏ tokendən götürülür: kimsə başqasının kartından pul göndərə bilməz.
    [Route("api/transfers")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("transfer")]
    public class TransfersController : ControllerBase
    {
        private readonly ITransferService _transferService;

        public TransfersController(ITransferService transferService)
        {
            _transferService = transferService;
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            }
        }

        // Pul köçürmür: yoxlama, komissiya və alıcının gizlədilmiş adı
        [HttpPost("preview")]
        public async Task<IActionResult> Preview([FromBody] TransferDto model)
        {
            return ToResponse(await _transferService.PreviewAsync(UserId, model));
        }

        [HttpPost]
        public async Task<IActionResult> Transfer([FromBody] TransferDto model)
        {
            return ToResponse(await _transferService.TransferAsync(UserId, model));
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
