using FluentValidation;
using Service.Helpers.Exceptions;

namespace Service.Helpers.Validators
{
    // Bütün admin validator-ları üçün ortaq qaydalar və yoxlamanı servisdə bir sətirlə çağırmaq üçün köməkçi
    public static class ValidationExtensions
    {
        // Yoxlama keçmirsə BadRequestException atır (middleware bütün mesajları 400 ilə qaytarır)
        public static async Task EnsureValidAsync<T>(this IValidator<T> validator, T model)
        {
            var result = await validator.ValidateAsync(model);
            if (!result.IsValid)
            {
                throw new BadRequestException(result.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }
        }

        // Mətn boş olmamalıdır və kənar boşluqlar silindikdən sonra limiti keçməməlidir (servis də dəyəri Trim ilə yazır)
        public static IRuleBuilderOptions<T, string?> RequiredText<T>(this IRuleBuilder<T, string?> rule, string name, int maxLength)
        {
            return rule
                .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage($"Enter the {name}.")
                .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= maxLength)
                    .WithMessage($"The {name} can be at most {maxLength} characters.");
        }

        // Düymənin ünvanı yalnız bu saytın yolu ola bilər (məs. /App/Transfer): "//sayt.com", "http:" və "javascript:" qəbul olunmur
        public static IRuleBuilderOptions<T, string?> SitePath<T>(this IRuleBuilder<T, string?> rule, int maxLength)
        {
            return rule
                .Must(IsSitePath).WithMessage("The link must be a path on this site, for example /App/Transfer.")
                .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= maxLength)
                    .WithMessage($"The link can be at most {maxLength} characters.");
        }

        private static bool IsSitePath(string? value)
        {
            var path = value?.Trim() ?? string.Empty;
            return path.StartsWith("/") && !path.StartsWith("//") && !path.Contains(':') && !path.Contains('\\');
        }
    }
}
