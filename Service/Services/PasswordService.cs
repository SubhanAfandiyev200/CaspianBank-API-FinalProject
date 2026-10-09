using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.Responses;
using Service.Helpers.Settings;
using Service.Services.Interfaces;
using System.Text;

namespace Service.Services
{
    public class PasswordService : IPasswordService
    {
        private const string InvalidLink = "This reset link is invalid or has expired.";

        private readonly IAccountRepository _accountRepository;
        private readonly IEmailSender _emailSender;
        private readonly IValidator<ForgotPasswordDto> _forgotValidator;
        private readonly IValidator<ResetPasswordDto> _resetValidator;
        private readonly FrontendSettings _frontend;
        private readonly SmtpSettings _smtp;
        private readonly ILogger<PasswordService> _logger;

        public PasswordService(IAccountRepository accountRepository,
                               IEmailSender emailSender,
                               IValidator<ForgotPasswordDto> forgotValidator,
                               IValidator<ResetPasswordDto> resetValidator,
                               IOptions<FrontendSettings> frontend,
                               IOptions<SmtpSettings> smtpSettings,
                               ILogger<PasswordService> logger)
        {
            _accountRepository = accountRepository;
            _emailSender = emailSender;
            _forgotValidator = forgotValidator;
            _resetValidator = resetValidator;
            _frontend = frontend.Value;
            _smtp = smtpSettings.Value;
            _logger = logger;
        }

        public async Task<OperationResponse> ForgotPasswordAsync(ForgotPasswordDto model)
        {
            var validation = await _forgotValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return Fail(validation.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            // Email mövcud olsa da olmasa da eyni cavab qayıdır (hesabların siyahısını çıxarmağa qarşı)
            var user = await _accountRepository.GetByEmailAsync(model.Email.Trim());
            if (user is null || user.IsRestricted)
            {
                return new OperationResponse
                {
                    IsSuccess = true
                };
            }

            string? devLink = null;
            try
            {
                var token = await _accountRepository.GeneratePasswordResetTokenAsync(user);
                var link = $"{_frontend.BaseUrl.TrimEnd('/')}/Account/ResetPassword"
                           + $"?email={Uri.EscapeDataString(user.Email!)}&token={Base64UrlEncode(token)}";
                devLink = link;

                await _emailSender.SendAsync(
                    user.Email!,
                    "Reset your Caspian Bank password",
                    $"<p>We received a request to reset your Caspian Bank password.</p>" +
                    $"<p><a href=\"{link}\">Choose a new password</a></p>" +
                    $"<p>The link expires in 30 minutes. If you did not ask for it, you can ignore this email.</p>");
            }
            catch (Exception ex)
            {
                // İstifadəçiyə xəta göstərmirik (cavab fərqi hesabın mövcudluğunu açardı), yalnız logda saxlayırıq
                _logger.LogError(ex, "Şifrə bərpası emaili göndərilə bilmədi: {Email}", model.Email);
            }

            // SMTP qurulmayıbsa (yalnız Development) link ekranda göstərilsin deyə qaytarılır
            return new OperationResponse
            {
                IsSuccess = true,
                DevLink = _smtp.ExposeCodes ? devLink : null
            };
        }

        public async Task<OperationResponse> ResetPasswordAsync(ResetPasswordDto model)
        {
            var validation = await _resetValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return Fail(validation.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }

            var user = await _accountRepository.GetByEmailAsync(model.Email.Trim());
            var token = Base64UrlDecode(model.Token);
            if (user is null || token is null)
            {
                return Fail(InvalidLink);
            }

            var result = await _accountRepository.ResetPasswordAsync(user, token, model.NewPassword);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(e => e.Code == "InvalidToken"))
                {
                    return Fail(InvalidLink);
                }

                return Fail(result.Errors.Select(e => e.Description).ToArray());
            }

            // Şifrə dəyişdi: uğursuz cəhd sayğacı və müvəqqəti blok sıfırlanır
            await _accountRepository.ResetFailedAttemptsAsync(user);

            return new OperationResponse
            {
                IsSuccess = true
            };
        }

        private static string Base64UrlEncode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string? Base64UrlDecode(string value)
        {
            try
            {
                var padded = value.Replace('-', '+').Replace('_', '/');
                padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
                return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static OperationResponse Fail(params string[] errors)
        {
            return new()
            {
                IsSuccess = false,
                Errors = errors
            };
        }
    }
}
