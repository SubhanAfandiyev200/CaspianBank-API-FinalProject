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
    }
}
