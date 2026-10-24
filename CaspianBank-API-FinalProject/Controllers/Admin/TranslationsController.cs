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

        // CMS səhifəsində dil seçiləndə həmin dilin tərcümələrini gətirir
        [HttpPost("lookup")]
        public async Task<IActionResult> Lookup([FromBody] LookupTranslationsDto model)
        {
            var result = await _service.LookupAsync(model);
            if (!result.IsSuccess)
            {
                return BadRequest(new { isSuccess = false, errors = result.Errors });
            }
            return Ok(result.Data);
        }

        // Bir dil üçün bir neçə mətnin tərcüməsini birlikdə yazır
        [HttpPut("content")]
        public async Task<IActionResult> SaveContent([FromBody] SaveContentTranslationsDto model)
        {
            var result = await _service.SaveContentAsync(model);
            if (!result.IsSuccess)
            {
                return BadRequest(new { isSuccess = false, errors = result.Errors });
            }
            return Ok(new { saved = result.Data });
        }
    }
}
