using FluentValidation;
using Service.Helpers.DTOs.CardTiers;

namespace Service.Helpers.Validators.CardTiers
{
    public class UpdateCardTierDtoValidator : AbstractValidator<CardTierUpdateDto>
    {
        // Bazada bu sütunlar decimal(18,2)-dir (CardTierConfigConfiguration): ən çox 2 rəqəm vergüldən sonra
        public UpdateCardTierDtoValidator()
        {
            RuleFor(x => x.IssueFee).AmountBetween("issue fee", 0m, 10000m);
            RuleFor(x => x.CashbackPercent).AmountBetween("cashback percent", 0m, 100m);

            // Köçürmə məbləği ən çox 100000 ola bilər (TransferService), limit ondan böyük mənasızdır
            RuleFor(x => x.TransferLimit).AmountBetween("transfer limit", 0m, 100000m);
            RuleFor(x => x.CommissionPercent).AmountBetween("commission percent", 0m, 100m);

            // Dizaynın bazada olub-olmadığını servis yoxlayır
            RuleFor(x => x.CardDesignId)
                .NotNull().WithMessage("Choose a card design.")
                .GreaterThan(0).WithMessage("Choose a card design.");
        }
    }

    public static class CardTierRules
    {
        // Pul və faiz sahələri üçün ortaq qayda: boş olmamalı, aralıqda olmalı, ən çox 2 onluq rəqəm
        public static IRuleBuilderOptions<T, decimal?> AmountBetween<T>(this IRuleBuilder<T, decimal?> rule, string name, decimal min, decimal max)
        {
            return rule
                .NotNull().WithMessage($"Enter the {name}.")
                .Must(value => value is null || (value >= min && value <= max))
                    .WithMessage($"The {name} must be between {min} and {max}.")
                .Must(value => value is null || value == decimal.Round(value.Value, 2))
                    .WithMessage($"The {name} can have at most 2 decimal places.");
        }
    }
}
