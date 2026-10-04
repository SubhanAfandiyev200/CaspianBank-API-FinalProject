using Microsoft.AspNetCore.Mvc;
using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Controllers.Client
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IHomeTickerService _tickerService;
        private readonly IBrandService _brandService;
        private readonly IAboutService _aboutService;
        private readonly IAboutPillarService _pillarService;
        private readonly IServiceSectionService _serviceSection;
        private readonly IServiceItemService _serviceItem;
        private readonly IBenefitSectionService _benefitSection;
        private readonly IBenefitItemService _benefitItem;

        public HomeController(IHomeTickerService tickerService,
                              IBrandService brandService,
                              IAboutService aboutService,
                              IAboutPillarService pillarService,
                              IServiceItemService serviceItem,
                              IServiceSectionService serviceSection,
                              IBenefitSectionService benefitSection,
                              IBenefitItemService benefitItem)
        {
            _tickerService = tickerService;
            _brandService = brandService;
            _pillarService = pillarService;
            _aboutService = aboutService;
            _serviceItem = serviceItem;
            _serviceSection = serviceSection;
            _benefitSection = benefitSection;
            _benefitItem = benefitItem;
        }

        [HttpGet("tickers")]
        public async Task<IActionResult> GetAllTickersAsync()
        {
            return Ok(await _tickerService.GetAllUIAsync());
        }
        [HttpGet("brands")]
        public async Task<IActionResult> GetAllBrandsAsync()
        {
            return Ok(await _brandService.GetAllUIAsync());
        }
        [HttpGet("about")]
        public async Task<IActionResult> GetAboutAsync()
        {
            var about = await _aboutService.GetUIAsync();
            if (about is null) return NotFound();
            return Ok(about);
        }
        [HttpGet("pillars")]
        public async Task<IActionResult> GetAllAboutPillarsAsync()
        {
            return Ok(await _pillarService.GetAllUIAsync());
        }
        [HttpGet("serviceSections")]
        public async Task<IActionResult> GetServiceSectionAsync()
        {
            var serviceSection = await _serviceSection.GetUIAsync();
            if (serviceSection is null) return NotFound();
            return Ok(serviceSection);
        }
        [HttpGet("serviceItems")]
        public async Task<IActionResult> GetAllServiceItemsAsync()
        {
            return Ok(await _serviceItem.GetAllUIAsync());
        }
        [HttpGet("benefitSections")]
        public async Task<IActionResult> GetBenefitSectionAsync()
        {
            var benefitSection = await _benefitSection.GetUIAsync();
            if (benefitSection is null) return NotFound();
            return Ok(benefitSection);
        }
        [HttpGet("benefitItems")]
        public async Task<IActionResult> GetAllBenefitItemsAsync()
        {
            return Ok(await _benefitItem.GetAllUIAsync());
        }
    }
}
