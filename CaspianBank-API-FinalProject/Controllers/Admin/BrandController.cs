using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Admin
{
    [Route("api/admin/brands")]
    [ApiController]
    [Authorize(Roles = "WebDesigner,SuperAdmin,Admin")]
    public class BrandController : ControllerBase
    {
        private readonly IBrandService _brandService;
        public BrandController(IBrandService brandService)
        {
            _brandService = brandService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllBrands()
        {
            return Ok(await _brandService.GetAllAsync());
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBrandDetailAsync([FromRoute] int id)
        {
            return Ok(await _brandService.GetDetailAsync(id));
        }
    }
}
