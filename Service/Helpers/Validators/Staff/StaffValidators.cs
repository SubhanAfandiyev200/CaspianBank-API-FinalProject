using Domain.Constants;
using FluentValidation;
using Service.Helpers.DTOs.Staff;
using System.Net.Mail;

namespace Service.Helpers.Validators.Staff
{
    public static class StaffRules
    {
        // SuperAdmin verilmir və alınmır; yalnız bu dörd rol təyin olunur
        public static readonly string[] AssignableRoles = { Roles.Admin, Roles.Accountant, Roles.CustomerSupport, Roles.Security };

        public static bool IsAssignable(string? role)
        {
            return AssignableRoles.Contains(role?.Trim());
        }
    }

    public class AddStaffDtoValidator : AbstractValidator<AddStaffDto>
    {
        public AddStaffDtoValidator()
        {
            RuleFor(x => x.Name).RequiredText("first name", 40);
            RuleFor(x => x.Surname).RequiredText("last name", 40);
            RuleFor(x => x.Email)
                .Must(e => !string.IsNullOrWhiteSpace(e)).WithMessage("Enter the work email.")
                .Must(e => string.IsNullOrWhiteSpace(e) || e.Trim().Length <= 100).WithMessage("The email can be at most 100 characters.")
                .Must(e => string.IsNullOrWhiteSpace(e) || IsEmail(e.Trim())).WithMessage("Enter a valid email address.");
            RuleFor(x => x.Role)
                .Must(StaffRules.IsAssignable).WithMessage("Choose Admin, Accountant, Customer support or Security.");
        }

        private static bool IsEmail(string value)
        {
            return MailAddress.TryCreate(value, out var address) && address.Address == value && value.Contains('.');
        }
    }

    public class ChangeStaffRoleDtoValidator : AbstractValidator<ChangeStaffRoleDto>
    {
        public ChangeStaffRoleDtoValidator()
        {
            RuleFor(x => x.Role)
                .Must(StaffRules.IsAssignable).WithMessage("Choose Admin, Accountant, Customer support or Security.");
        }
    }
}
