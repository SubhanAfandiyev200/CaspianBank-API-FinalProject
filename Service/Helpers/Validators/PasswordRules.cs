using FluentValidation;

namespace Service.Helpers.Validators
{
    public static class PasswordRules
    {
        // Program.cs-dəki Identity şifrə qaydaları ilə üst-üstə düşür
        public static IRuleBuilderOptions<T, string> MustBeStrongPassword<T>(this IRuleBuilder<T, string> rule)
        {
            return rule
                .NotEmpty().WithMessage("Enter a password.")
                .MinimumLength(6).WithMessage("Use at least 6 characters.")
                .Matches("[A-Z]").WithMessage("Include an uppercase letter.")
                .Matches("[a-z]").WithMessage("Include a lowercase letter.")
                .Matches("[0-9]").WithMessage("Include a number.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Include a special character.");
        }
    }
}
