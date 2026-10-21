using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Translations;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Tərcümələrin idarəsi: SuperAdmin və Admin. Açar ingilis mətnin özüdür; boş saxlanan tərcümə silinir (ingilis mətn göstərilir)
    [Route("api/admin/translations")]
    [ApiController]
    [Authorize(Roles = Roles.SuperAdmin + "," + Roles.Admin)]
    public class TranslationsController : ControllerBase
    {
        private readonly ITranslationService _service;

        public TranslationsController(ITranslationService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetPage([FromQuery] TranslationFilterDto filter)
        {
            var result = await _service.GetPageAsync(filter);
            if (!result.IsSuccess)
            {
                return BadRequest(new { isSuccess = false, errors = result.Errors });
            }
            return Ok(result.Data);
        }

        [HttpPut]
        public async Task<IActionResult> Save([FromBody] SaveTranslationDto model)
        {
            var result = await _service.SaveAsync(model);
            if (!result.IsSuccess)
            {
                return BadRequest(new { isSuccess = false, errors = result.Errors });
            }
            return Ok(result.Data);
        }
    }
}
