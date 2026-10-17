using Domain.Constants;
using Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.Responses;
using Service.Helpers.Settings;
using Service.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Service.Services
{
    public class OtpService : IOtpService
    {
        private const int MaxAttempts = 5;
        private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan VerificationLifetime = TimeSpan.FromMinutes(15);

        private readonly IEmailOtpRepository _otpRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly IEmailSender _emailSender;
        private readonly IValidator<SendOtpDto> _sendValidator;
        private readonly IValidator<VerifyOtpDto> _verifyValidator;
        private readonly ILogger<OtpService> _logger;
        private readonly byte[] _hashKey;
        private readonly SmtpSettings _smtp;

        public OtpService(IEmailOtpRepository otpRepo,
                          IAccountRepository accountRepo,
                          IEmailSender emailSender,
                          IValidator<SendOtpDto> sendValidator,
                          IValidator<VerifyOtpDto> verifyValidator,
                          IOptions<JwtSettings> jwtSettings,
                          IOptions<SmtpSettings> smtpSettings,
                          ILogger<OtpService> logger)
        {
            _otpRepo = otpRepo;
            _accountRepo = accountRepo;
            _emailSender = emailSender;
            _sendValidator = sendValidator;
            _verifyValidator = verifyValidator;
            _logger = logger;
            _smtp = smtpSettings.Value;
            // Hash açarı serverdədir (bazada yox): baza sızsa belə 6 rəqəmli kodu oflayn tapmaq olmur
            _hashKey = Encoding.UTF8.GetBytes(jwtSettings.Value.Key);
        }

        // ---- Qeydiyyat kodu ----

        public async Task<SendOtpResponse> SendOtpAsync(SendOtpDto model)
        {
            var validation = await _sendValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return SendFail(validation.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var email = Normalize(model.Email);

            if (await _accountRepo.GetByEmailAsync(email) is not null)
            {
                return SendFail("This email is already registered. Please log in.");
            }

            return await IssueCodeAsync(email, OtpPurposes.Register,
                "Your Caspian Bank verification code",
                "Your Caspian Bank verification code is:");
        }

        public async Task<VerifyOtpResponse> VerifyOtpAsync(VerifyOtpDto model)
        {
            var validation = await _verifyValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return VerifyFail(validation.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            return await CheckCodeAsync(Normalize(model.Email), OtpPurposes.Register, model.Code, issueToken: true);
        }

        public async Task<bool> IsVerifiedAsync(string email, string verificationToken)
        {
            if (string.IsNullOrWhiteSpace(verificationToken))
            {
                return false;
            }
            var normalized = Normalize(email);
            var record = await _otpRepo.GetVerifiedAsync(
                normalized,
                Hash("otp-verification", normalized, verificationToken),
                DateTime.UtcNow - VerificationLifetime);
            return record is not null;
        }

        public async Task ConsumeVerificationAsync(string email, string verificationToken)
        {
            var normalized = Normalize(email);
            var record = await _otpRepo.GetVerifiedAsync(
                normalized,
                Hash("otp-verification", normalized, verificationToken),
                DateTime.UtcNow - VerificationLifetime);
            if (record is null)
            {
                return;
            }

            record.VerificationTokenHash = null;
            await _otpRepo.UpdateAsync(record);
        }

        // ---- Giriş etmiş istifadəçinin əməliyyat kodu (kartı bloklamaq / blokdan çıxarmaq və s.) ----

        public Task<SendOtpResponse> SendCodeAsync(string email, string purpose, string subject, string intro)
        {
            return IssueCodeAsync(Normalize(email), purpose, subject, intro);
        }

        public Task<VerifyOtpResponse> ConfirmCodeAsync(string email, string purpose, string code)
        {
            return CheckCodeAsync(Normalize(email), purpose, code ?? string.Empty, issueToken: false);
        }

        // ---- ortaq hissə ----

        // Kod yaradır, hash-ini saxlayır və emailə göndərir. Eyni email + məqsəd üçün 60 saniyədən tez yeni kod verilmir
        private async Task<SendOtpResponse> IssueCodeAsync(string email, string purpose, string subject, string intro)
        {
            // Çox tez-tez yeni kod istəməyin qarşısı (email spam-ı və kod ələ keçirmə cəhdləri)
            var latest = await _otpRepo.GetLatestAsync(email, purpose);
            if (latest is not null)
            {
                var wait = ResendCooldown - (DateTime.UtcNow - latest.CreatedAt);
                if (wait > TimeSpan.Zero)
                {
                    var seconds = (int)Math.Ceiling(wait.TotalSeconds);
                    return new SendOtpResponse
                    {
                        IsSuccess = false,
                        Errors = new[] { $"Please wait {seconds} seconds before requesting a new code." },
                        RetryAfterSeconds = seconds
                    };
                }
            }

            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

            await _otpRepo.InvalidateActiveAsync(email, purpose);
            await _otpRepo.AddAsync(new EmailOtp
            {
                Email = email,
                Purpose = purpose,
                CodeHash = HashCode(purpose, email, code),
                ExpiresAt = DateTime.UtcNow.Add(CodeLifetime)
            });

            try
            {
                await _emailSender.SendAsync(
                    email,
                    subject,
                    $"<p>{intro}</p><h2 style=\"letter-spacing:4px\">{code}</h2>" +
                    $"<p>It expires in {(int)CodeLifetime.TotalMinutes} minutes. If you did not request it, ignore this email.</p>");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OTP emaili göndərilə bilmədi: {Email}", email);
                return SendFail("We could not send the email. Please try again later.");
            }

            return new SendOtpResponse
            {
                IsSuccess = true,
                DevCode = _smtp.ExposeCodes ? code : null
            };
        }

        // Kodu yoxlayır. issueToken: true (qeydiyyat) olsa Register üçün təsdiq tokeni də yaradır; əks halda kod sadəcə istifadə olunmuş sayılır
        private async Task<VerifyOtpResponse> CheckCodeAsync(string email, string purpose, string code, bool issueToken)
        {
            var otp = await _otpRepo.GetLatestActiveAsync(email, purpose);
            if (otp is null)
            {
                return VerifyFail("The code has expired or is invalid. Please request a new one.");
            }

            otp.Attempts++;
            if (otp.Attempts > MaxAttempts)
            {
                otp.IsUsed = true;
                await _otpRepo.UpdateAsync(otp);
                return VerifyFail("Too many attempts. Please request a new code.");
            }

            var expected = Encoding.UTF8.GetBytes(otp.CodeHash);
            var actual = Encoding.UTF8.GetBytes(HashCode(purpose, email, code));
            if (!CryptographicOperations.FixedTimeEquals(expected, actual))
            {
                await _otpRepo.UpdateAsync(otp);
                return VerifyFail("The code is incorrect.");
            }

            otp.IsUsed = true;
            string? token = null;
            if (issueToken)
            {
                token = Base64Url(RandomNumberGenerator.GetBytes(32));
                otp.VerificationTokenHash = Hash("otp-verification", email, token);
                otp.VerifiedAt = DateTime.UtcNow;
            }
            await _otpRepo.UpdateAsync(otp);

            return new VerifyOtpResponse
            {
                IsSuccess = true,
                VerificationToken = token
            };
        }

        private static string Normalize(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        // Kodun hash-i məqsədə də bağlıdır: bir məqsəd üçün göndərilən kod başqasında işləmir.
        // Qeydiyyat kodu əvvəlki hash adını saxlayır (köhnə kodlar etibarlı qalsın)
        private string HashCode(string purpose, string email, string code)
        {
            var name = purpose == OtpPurposes.Register ? "email-otp" : "email-otp:" + purpose;
            return Hash(name, email, code);
        }

        private string Hash(string purpose, string email, string value)
        {
            using var hmac = new HMACSHA256(_hashKey);
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{purpose}|{email}|{value}"));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string Base64Url(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static SendOtpResponse SendFail(params string[] errors)
        {
            return new()
            {
                IsSuccess = false,
                Errors = errors
            };
        }

        private static VerifyOtpResponse VerifyFail(params string[] errors)
        {
            return new()
            {
                IsSuccess = false,
                Errors = errors
            };
        }
    }
}
