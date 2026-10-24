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
        private readonly IValidator<CheckIdentityDto> _checkIdentityValidator;
        private readonly ICustomerRepository _customerRepository;
        private readonly ITokenService _tokenService;
        private readonly IOtpService _otpService;
        private readonly ICardService _cardService;
        private readonly ISecurityMonitor _security;

        private const string InvalidCredentials = "Invalid email or password.";

        public AccountService(IAccountRepository accountRepository,
                              IValidator<RegisterDto> registerValidator,
                              IValidator<LoginDto> loginValidator,
                              IValidator<CheckEmailDto> checkEmailValidator,
                              IValidator<CheckIdentityDto> checkIdentityValidator,
                              ICustomerRepository customerRepository,
                              ITokenService tokenService,
                              IOtpService otpService,
                              ICardService cardService,
                              ISecurityMonitor security)
        {
            _accountRepository = accountRepository;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
            _checkEmailValidator = checkEmailValidator;
            _checkIdentityValidator = checkIdentityValidator;
            _customerRepository = customerRepository;
            _tokenService = tokenService;
            _otpService = otpService;
            _cardService = cardService;
            _security = security;
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

            // Vətəndaş reyestrdə tapılmalıdır (FİN + vəsiqə nömrəsi eyni adama aid), sağ olmalıdır və hesabı olmamalıdır
            var (customer, citizenError) = await FindCitizenAsync(model.Fin, model.IdCardNumber, model.Name, model.Surname);
            if (customer is null)
            {
                return Fail(citizenError!);
            }

            var phone = NormalizePhone(model.PhoneNumber);
            if (await _accountRepository.PhoneInUseAsync(phone))
            {
                return Fail("This phone number is already used by another account.");
            }

            // Ad, soyad və doğum tarixi reyestrdən gəlir: istifadəçi onları yaza bilmir
            var user = new AppUser
            {
                Name = customer.Name,
                Surname = customer.Surname,
                BirthDay = customer.BirthDate,
                FinKod = customer.Fin,
                CustomerId = customer.Id,
                Email = model.Email,
                UserName = model.Email, // login email ilədir
                PhoneNumber = phone,
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

            // İlk iki kart (Standard + Cashback) avtomatik açılır. Alınmasa da hesab hazırdır:
            // müştəri girəndən sonra Cards səhifəsindəki düymə ilə yenidən yarada bilər
            await _cardService.CreateInitialCardsAsync(user.Id);

            return new RegisterResponse
            {
                IsSuccess = true
            };
        }

        public async Task<CheckIdentityResponse> CheckIdentityAsync(CheckIdentityDto model)
        {
            var validation = await _checkIdentityValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return new CheckIdentityResponse
                {
                    IsSuccess = false,
                    Errors = validation.Errors.Select(e => e.ErrorMessage).ToArray()
                };
            }

            var (customer, error) = await FindCitizenAsync(model.Fin, model.IdCardNumber, model.Name, model.Surname);
            if (customer is null)
            {
                return new CheckIdentityResponse
                {
                    IsSuccess = false,
                    Errors = new[] { error! }
                };
            }

            return new CheckIdentityResponse
            {
                IsSuccess = true,
                Name = customer.Name,
                MaskedSurname = customer.Surname.Substring(0, 1) + ".",
                BirthYear = customer.BirthDate.Year,
                PhotoUrl = customer.PhotoUrl
            };
        }

        public async Task<ProfileDto?> GetProfileAsync(string userId)
        {
            var user = await _accountRepository.GetByIdAsync(userId);
            if (user is null)
            {
                return null;
            }

            return new ProfileDto
            {
                Name = user.Name,
                Surname = user.Surname,
                Fin = user.FinKod,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                PhotoUrl = await GetPhotoUrlAsync(user)
            };
        }

        // Şəkil reyestrdəki vətəndaşdadır (işçilərin reyestr sətri və şəkli yoxdur)
        private async Task<string?> GetPhotoUrlAsync(AppUser user)
        {
            if (user.CustomerId is null)
            {
                return null;
            }

            var customer = await _customerRepository.GetByIdAsync(user.CustomerId.Value);
            return customer?.PhotoUrl;
        }

        // Reyestrdən vətəndaşı tapır. Tapılmasa və ya qeydiyyat olmazsa səbəb mətni qayıdır
        private async Task<(Customer? Customer, string? Error)> FindCitizenAsync(string fin, string idCardNumber, string name, string surname)
        {
            var customer = await _customerRepository.GetByFinAndIdCardAsync(fin.Trim().ToUpperInvariant(), idCardNumber.Trim().ToUpperInvariant());

            // FİN, vəsiqə nömrəsi, ad və soyad eyni vətəndaşa uyğun olmalıdır. Hansının səhv olduğunu demirik (təxmin etməni çətinləşdirir)
            if (customer is null || Fold(customer.Name) != Fold(name) || Fold(customer.Surname) != Fold(surname))
            {
                return (null, "We couldn't find a citizen with these details. Check the FIN, ID card number, first name and last name and try again.");
            }

            if (customer.DeathDate is not null)
            {
                return (null, "This ID record can't be used to open an account. If you think this is a mistake, please contact the bank.");
            }

            if (await _accountRepository.CustomerHasAccountAsync(customer.Id))
            {
                return (null, "An account already exists for this person. Log in instead.");
            }

            return (customer, null);
        }

        // Adları müqayisə üçün sadələşdirir: böyük/kiçik hərf, boşluqlar və Azərbaycan hərfləri fərqləndirilmir
        // (ə=e, ı=i, ö=o, ü=u, ş=s, ç=c, ğ=g), ki Azərbaycan klaviaturası olmayan adam da düz yaza bilsin
        private static string Fold(string value)
        {
            var text = value.Trim().ToLowerInvariant().Replace('ə', 'e').Replace('ı', 'i');
            var decomposed = text.Normalize(System.Text.NormalizationForm.FormD);
            var letters = decomposed.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark);
            return string.Concat(letters).Replace(" ", string.Empty);
        }

        // 501234567 və 0501234567 eyni nömrədir: bazaya həmişə 0-lu yazılır
        private static string NormalizePhone(string phone)
        {
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            if (digits.StartsWith('0'))
            {
                return digits;
            }
            return "0" + digits;
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

                // Təkrarlanan yanlış şifrədən sonra hesab müvəqqəti bloklanıbsa Security siqnal görür
                if (await _accountRepository.IsLockedOutAsync(user))
                {
                    await _security.OnAccountLockedAsync(user.Id);
                }
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
            return new CheckEmailResponse
            {
                IsSuccess = true,
                Exists = user is not null
            };
        }

        private static RegisterResponse Fail(params string[] errors)
        {
            return new()
            {
                IsSuccess = false,
                Errors = errors
            };
        }

        private static LoginResponse FailLogin(params string[] errors)
        {
            return new()
            {
                IsSuccess = false,
                Errors = errors
            };
        }
    }
}
