using Domain.Constants;
using FluentValidation;
using Domain.Entities;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.Responses;
using Service.Services.Interfaces;

namespace Service.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IValidator<RegisterDto> _registerValidator;
        private readonly IValidator<LoginDto> _loginValidator;
        private readonly IValidator<CheckEmailDto> _checkEmailValidator;
        private readonly ITokenService _tokenService;
        private readonly IOtpService _otpService;

        private const string InvalidCredentials = "Invalid email or password.";

        public AccountService(IAccountRepository accountRepository,
                              IValidator<RegisterDto> registerValidator,
                              IValidator<LoginDto> loginValidator,
                              IValidator<CheckEmailDto> checkEmailValidator,
                              ITokenService tokenService,
                              IOtpService otpService)
        {
            _accountRepository = accountRepository;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
            _checkEmailValidator = checkEmailValidator;
            _tokenService = tokenService;
            _otpService = otpService;
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterDto model)
        {
            var validation = await _registerValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return Fail(validation.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            // Email OTP ilə təsdiqlənməyibsə qeydiyyat olmaz
            if (!await _otpService.IsVerifiedAsync(model.Email, model.VerificationToken))
            {
                return Fail("Email verification is missing or has expired. Please verify your email again.");
            }

            var user = new AppUser
            {
                Name = model.Name,
                Surname = model.Surname,
                Email = model.Email,
                UserName = model.Email, // login email ilədir
                PhoneNumber = model.PhoneNumber,
                BirthDay = model.BirthDay,
                EmailConfirmed = true // email OTP ilə qeydiyyatdan əvvəl təsdiqlənir
            };

            var result = await _accountRepository.RegisterAsync(user, model.Password);
            if (!result.Succeeded)
            {
                return Fail(result.Errors.Select(e => e.Description).ToArray());
            }

            var roleResult = await _accountRepository.AddToRoleAsync(user, Roles.Customer);
            if (!roleResult.Succeeded)
            {
                return Fail(roleResult.Errors.Select(e => e.Description).ToArray());
            }

            // Token yalnız bir dəfə işləyir
            await _otpService.ConsumeVerificationAsync(model.Email, model.VerificationToken);

            return new RegisterResponse { IsSuccess = true };
        }

        public async Task<LoginResponse> LoginAsync(LoginDto model)
        {
            var validation = await _loginValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return FailLogin(validation.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var user = await _accountRepository.GetByEmailAsync(model.Email.Trim());
            if (user is null)
            {
                return FailLogin(InvalidCredentials); // email yoxdursa da eyni mesaj (user enumeration qarşısı)
            }

            if (await _accountRepository.IsLockedOutAsync(user))
            {
                return FailLogin("Too many failed attempts. Try again in a few minutes.");
            }

            if (!await _accountRepository.CheckPasswordAsync(user, model.Password))
            {
                await _accountRepository.RegisterFailedAttemptAsync(user);
                return FailLogin(InvalidCredentials);
            }

            await _accountRepository.ResetFailedAttemptsAsync(user);

            // Admin tərəfindən dondurulmuş (freeze) hesab şifrə düzgün olsa da daxil ola bilmir
            if (user.IsRestricted)
            {
                return FailLogin("Your account is restricted. Please contact support.");
            }

            var roles = await _accountRepository.GetRolesAsync(user);
            var (token, expiresAt) = _tokenService.CreateToken(user, roles);

            return new LoginResponse
            {
                IsSuccess = true,
                Token = token,
                ExpiresAt = expiresAt,
                UserId = user.Id,
                Email = user.Email,
                FullName = $"{user.Name} {user.Surname}".Trim(),
                Roles = roles.ToArray()
            };
        }

        public async Task<CheckEmailResponse> CheckEmailAsync(CheckEmailDto model)
        {
            var validation = await _checkEmailValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return new CheckEmailResponse
                {
                    IsSuccess = false,
                    Errors = validation.Errors.Select(e => e.ErrorMessage).ToArray()
                };
            }

            var user = await _accountRepository.GetByEmailAsync(model.Email.Trim());
            return new CheckEmailResponse { IsSuccess = true, Exists = user is not null };
        }

        private static RegisterResponse Fail(params string[] errors)
        {
            return new() { IsSuccess = false, Errors = errors };
        }

        private static LoginResponse FailLogin(params string[] errors)
        {
            return new() { IsSuccess = false, Errors = errors };
        }
    }
}
