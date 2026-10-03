using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Service.Helpers.DTOs.Accounts;
using Service.Services.Interfaces;
using System.Security.Claims;

namespace CaspianBank_API_FinalProject.Controllers.Client
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly IOtpService _otpService;
        private readonly IPasswordService _passwordService;

        public AccountController(IAccountService accountService, IOtpService otpService, IPasswordService passwordService)
        {
            _accountService = accountService;
            _otpService = otpService;
            _passwordService = passwordService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            var result = await _accountService.RegisterAsync(model);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [EnableRateLimiting("auth")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var result = await _accountService.LoginAsync(model);
            if (!result.IsSuccess)
                return Unauthorized(result);

            return Ok(result);
        }

        // Email sistemdə varmı? Welcome səhifəsi buna görə Login və ya Register-ə yönləndirir.
        // Cavab emailin mövcudluğunu açır (user enumeration), ona görə rate limiting məcburidir.
        [EnableRateLimiting("auth")]
        [HttpPost("check-email")]
        public async Task<IActionResult> CheckEmail([FromBody] CheckEmailDto model)
        {
            var result = await _accountService.CheckEmailAsync(model);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        // Yeni email-ə 6 rəqəmli kod göndərir (emailin sahibi olduğunu təsdiqləmək üçün)
        [EnableRateLimiting("auth")]
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpDto model)
        {
            var result = await _otpService.SendOtpAsync(model);
            if (result.IsSuccess)
                return Ok(result);

            // Çox tez yeni kod istəyibsə 429, qalan hallarda 400
            if (result.RetryAfterSeconds > 0)
                return StatusCode(StatusCodes.Status429TooManyRequests, result);

            return BadRequest(result);
        }

        // Kodu yoxlayır; düzgündürsə Register üçün təsdiq tokeni qaytarır
        [EnableRateLimiting("auth")]
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto model)
        {
            var result = await _otpService.VerifyOtpAsync(model);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        // Şifrəni unutdum: emailə sıfırlama linki göndərir. Email mövcud olmasa da cavab eynidir.
        [EnableRateLimiting("auth")]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            var result = await _passwordService.ForgotPasswordAsync(model);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        // Emaildəki linkdən gələn token ilə yeni şifrə təyin edir
        [EnableRateLimiting("auth")]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            var result = await _passwordService.ResetPasswordAsync(model);
            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        // Tokenin işlədiyini yoxlamaq üçün: Authorization: Bearer <token>
        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                Id = User.FindFirstValue(ClaimTypes.NameIdentifier),
                Email = User.FindFirstValue(ClaimTypes.Email),
                Name = User.FindFirstValue(ClaimTypes.Name),
                Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value)
            });
        }
    }
}
