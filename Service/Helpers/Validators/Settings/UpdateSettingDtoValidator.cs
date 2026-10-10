using Domain.Constants;
using FluentValidation;
using Service.Helpers.DTOs.Settings;

namespace Service.Helpers.Validators.Settings
{
    public class UpdateSettingDtoValidator : AbstractValidator<UpdateSettingDto>
    {
        public UpdateSettingDtoValidator()
        {
            // 500 = bazadakı Settings.Value uzunluğu (SettingConfiguration)
            RuleFor(x => x.Value).RequiredText("value", 500);

            // Açara görə daha sıx qaydalar (servis açarı DTO-ya yazır)
            LimitFor(SettingKeys.CompanyName, "company name", 100);
            LimitFor(SettingKeys.Address, "address", 200);
            LimitFor(SettingKeys.FooterTitle, "footer title", 200);
            LimitFor(SettingKeys.Copyright, "copyright line", 100);

            When(x => x.Key == SettingKeys.Email, () =>
            {
                RuleFor(x => x.Value)
                    .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 100)
                        .WithMessage("The email can be at most 100 characters.")
                    .EmailAddress().WithMessage("Enter a valid email address.");
            });

            When(x => x.Key == SettingKeys.PhoneNumber, () =>
            {
                RuleFor(x => x.Value)
                    .Must(value => string.IsNullOrWhiteSpace(value) || System.Text.RegularExpressions.Regex.IsMatch(value.Trim(), @"^\+?[0-9][0-9 ()\-]{5,19}$"))
                        .WithMessage("Enter a valid phone number, for example +994 12 345 67 89.");
            });
        }

        private void LimitFor(string key, string name, int maxLength)
        {
            When(x => x.Key == key, () =>
            {
                RuleFor(x => x.Value)
                    .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= maxLength)
                        .WithMessage($"The {name} can be at most {maxLength} characters.");
            });
        }
    }
}
