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

            foreach (var socialKey in new[] { SettingKeys.InstagramUrl, SettingKeys.FacebookUrl, SettingKeys.TwitterUrl, SettingKeys.LinkedInUrl })
            {
                LinkFor(socialKey);
            }

            CoordinateFor(SettingKeys.MapLatitude, "latitude", -90, 90);
            CoordinateFor(SettingKeys.MapLongitude, "longitude", -180, 180);

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

        // Sosial şəbəkə linki: https:// və ya http:// ilə başlayan düzgün ünvan (javascript: kimi təhlükəli ünvanlar keçmir)
        private void LinkFor(string key)
        {
            When(x => x.Key == key, () =>
            {
                RuleFor(x => x.Value)
                    .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 200)
                        .WithMessage("The link can be at most 200 characters.")
                    .Must(value => Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
                        .WithMessage("Enter a full link that starts with https://, for example https://www.instagram.com/caspianbank.");
            });
        }


        // Xəritənin koordinatı: nöqtə ilə yazılmış rəqəm (40.3730), aralıq daxilində. Vergül qəbul edilmir ki, ünvanda səhv olmasın
        private void CoordinateFor(string key, string name, double min, double max)
        {
            When(x => x.Key == key, () =>
            {
                RuleFor(x => x.Value)
                    .Must(value => double.TryParse(value?.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var number)
                                   && number >= min && number <= max)
                        .WithMessage($"Enter the {name} as a number between {min} and {max}, with a dot, for example 40.3730.");
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
