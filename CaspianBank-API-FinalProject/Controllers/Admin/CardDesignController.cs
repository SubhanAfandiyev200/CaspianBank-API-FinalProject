using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.CardDesigns;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    // Home səhifəsindəki kart dizaynlarını idarə edir (WebDesigner, Admin, SuperAdmin)
    [Route("api/admin/card-designs")]
    [ApiController]
    [Authorize(Roles = Roles.WebDesigner + "," + Roles.Admin + "," + Roles.SuperAdmin)]
    public class CardDesignController : ControllerBase
    {
        private readonly ICardDesignService _service;

        public CardDesignController(ICardDesignService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            return Ok(await _service.GetDetailAsync(id));
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CreateCardDesignDto request)
        {
            await _service.CreateAsync(request);
            return Ok();
        }

        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateCardDesignDto request)
        {
            await _service.UpdateAsync(id, request);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
