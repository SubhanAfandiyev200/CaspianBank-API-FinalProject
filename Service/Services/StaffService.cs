using Domain.Constants;
using Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Staff;
using Service.Helpers.Responses;
using Service.Helpers.Settings;
using Service.Helpers.Validators.Staff;
using Service.Services.Interfaces;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Service.Services
{
    public class StaffService : IStaffService
    {
        private readonly IStaffRepository _staff;
        private readonly IAccountRepository _accounts;
        private readonly IEmailSender _emailSender;
        private readonly IValidator<AddStaffDto> _addValidator;
        private readonly IValidator<ChangeStaffRoleDto> _roleValidator;
        private readonly FrontendSettings _frontend;
        private readonly SmtpSettings _smtp;
        private readonly ILogger<StaffService> _logger;

        public StaffService(IStaffRepository staff,
                            IAccountRepository accounts,
                            IEmailSender emailSender,
                            IValidator<AddStaffDto> addValidator,
                            IValidator<ChangeStaffRoleDto> roleValidator,
                            IOptions<FrontendSettings> frontend,
                            IOptions<SmtpSettings> smtp,
                            ILogger<StaffService> logger)
        {
            _staff = staff;
            _accounts = accounts;
            _emailSender = emailSender;
            _addValidator = addValidator;
            _roleValidator = roleValidator;
            _frontend = frontend.Value;
            _smtp = smtp.Value;
            _logger = logger;
        }

        public async Task<StaffListDto> GetListAsync()
        {
            var rows = await _staff.GetAllAsync();
            return new StaffListDto
            {
                Items = rows.Select(r => ToDto(r.User, r.Role)).ToList(),
                AssignableRoles = StaffRules.AssignableRoles.ToList()
            };
        }

        public async Task<ServiceResult<StaffDto>> AddAsync(AddStaffDto model)
        {
            var validation = await _addValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<StaffDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }

            var email = model.Email.Trim();
            if (await _accounts.GetByEmailAsync(email) is not null)
            {
                return ServiceResult<StaffDto>.Fail("An account with this email already exists.");
            }

            var user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Name = model.Name.Trim(),
                Surname = model.Surname.Trim(),
                BirthDay = new DateTime(1990, 1, 1)
            };

            // Təsadüfi şifrə heç kimə göstərilmir: işçi emaildəki link ilə özü şifrə seçir
            var created = await _staff.CreateAsync(user, TemporaryPassword(), model.Role.Trim());
            if (!created.Succeeded)
            {
                return ServiceResult<StaffDto>.Fail(created.Errors.Select(e => e.Description).ToArray());
            }

            var dto = ToDto(user, model.Role.Trim());
            dto.InviteLink = await SendInviteAsync(user);
            return ServiceResult<StaffDto>.Ok(dto);
        }

        public async Task<ServiceResult<StaffDto>> ChangeRoleAsync(string actorUserId, string staffId, ChangeStaffRoleDto model)
        {
            var validation = await _roleValidator.ValidateAsync(model);
            if (!validation.IsValid)
            {
                return ServiceResult<StaffDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }

            var (user, role, error) = await FindTargetAsync(actorUserId, staffId);
            if (user is null)
            {
                return error!;
            }

            var newRole = model.Role.Trim();
            if (role == newRole)
            {
                return ServiceResult<StaffDto>.Ok(ToDto(user, role));
            }

            var changed = await _staff.ChangeRoleAsync(user, newRole);
            if (!changed.Succeeded)
            {
                return ServiceResult<StaffDto>.Fail(changed.Errors.Select(e => e.Description).ToArray());
            }
            return ServiceResult<StaffDto>.Ok(ToDto(user, newRole));
        }

        public async Task<ServiceResult<StaffDto>> SetActiveAsync(string actorUserId, string actorName, string staffId, bool active)
        {
            var (user, role, error) = await FindTargetAsync(actorUserId, staffId);
            if (user is null)
            {
                return error!;
            }

            user.IsRestricted = !active;
            user.RestrictionReason = active ? null : "Deactivated by the super admin";
            user.RestrictedAt = active ? null : DateTime.UtcNow;
            user.RestrictedBy = active ? null : actorName;

            var updated = await _staff.UpdateAsync(user);
            if (!updated.Succeeded)
            {
                return ServiceResult<StaffDto>.Fail(updated.Errors.Select(e => e.Description).ToArray());
            }
            return ServiceResult<StaffDto>.Ok(ToDto(user, role!));
        }

        // ---------- köməkçilər ----------

        // Hədəf işçi tapılmalıdır; SuperAdmin və işçinin öz hesabı dəyişdirilmir (özünü bloklamaq və ya yetkisini itirmək olmasın)
        private async Task<(AppUser? User, string? Role, ServiceResult<StaffDto>? Error)> FindTargetAsync(string actorUserId, string staffId)
        {
            var user = await _staff.FindAsync(staffId);
            if (user is null)
            {
                return (null, null, ServiceResult<StaffDto>.NotFound());
            }

            var role = await _staff.GetRoleAsync(user);
            if (role is null)
            {
                // Müştəridir, işçi deyil
                return (null, null, ServiceResult<StaffDto>.NotFound());
            }
            if (role == Roles.SuperAdmin)
            {
                return (null, null, ServiceResult<StaffDto>.Fail("The super admin account can't be changed here."));
            }
            if (user.Id == actorUserId)
            {
                return (null, null, ServiceResult<StaffDto>.Fail("You can't change your own account."));
            }
            return (user, role, null);
        }

        private async Task<string?> SendInviteAsync(AppUser user)
        {
            string? link = null;
            try
            {
                var token = await _accounts.GeneratePasswordResetTokenAsync(user);
                link = $"{_frontend.BaseUrl.TrimEnd('/')}/Account/ResetPassword"
                       + $"?email={Uri.EscapeDataString(user.Email!)}&token={Base64UrlEncode(token)}";

                await _emailSender.SendAsync(
                    user.Email!,
                    "Your Caspian Bank staff account",
                    $"<p>Hello {WebUtility.HtmlEncode(user.Name)},</p>" +
                    "<p>A Caspian Bank staff account was created for you. Choose your password to start:</p>" +
                    $"<p><a href=\"{link}\">Choose my password</a></p>" +
                    "<p>The link expires in 30 minutes. If it has expired, open the login page and use \"Forgot password\" with this email.</p>");
            }
            catch (Exception ex)
            {
                // Hesab artıq yaradılıb; email getməsə də işçi "Forgot password" ilə şifrə seçə bilər
                _logger.LogError(ex, "İşçi dəvət emaili göndərilə bilmədi: {Email}", user.Email);
            }
            return _smtp.ExposeCodes ? link : null;
        }

        private static StaffDto ToDto(AppUser user, string role)
        {
            return new StaffDto
            {
                Id = user.Id,
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email ?? string.Empty,
                Role = role,
                IsActive = !user.IsRestricted,
                IsSuperAdmin = role == Roles.SuperAdmin,
                CreatedAt = user.CreatedAt
            };
        }

        // Identity qaydalarını ödəyən uzun təsadüfi şifrə
        private static string TemporaryPassword()
        {
            return "Cb!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)) + "a";
        }

        private static string Base64UrlEncode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
