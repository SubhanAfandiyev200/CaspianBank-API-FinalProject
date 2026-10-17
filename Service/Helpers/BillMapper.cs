using Domain.Bills;
using Domain.Entities;
using Domain.Enums;
using Repository.Results;
using Service.Helpers.DTOs.Bills;

namespace Service.Helpers
{
    // Ödəniş modellərini DTO-ya çevirir
    public static class BillMapper
    {
        public static BillProviderDto ToProviderDto(BillProvider provider)
        {
            var isMobile = provider.Category == BillCategory.Mobile;
            return new BillProviderDto
            {
                Id = provider.Id,
                Code = provider.Code,
                Name = provider.Name,
                Category = provider.Category.ToString(),
                LogoUrl = provider.LogoUrl,
                IdentifierLabel = provider.IdentifierLabel,
                IdentifierHint = isMobile ? "055 123 45 67" : provider.IdentifierLength + " digits",
                PrefixesText = isMobile ? BillingEngine.PrefixesText(provider.Prefixes) : null,
                IdentifierLength = provider.IdentifierLength,
                UnitName = provider.UnitName,
                UnitPrice = provider.UnitPrice,
                MinPayment = provider.MinPayment,
                MaxPayment = provider.MaxPayment
            };
        }

        public static BillProviderAdminDto ToAdminDto(BillProvider provider)
        {
            return new BillProviderAdminDto
            {
                Id = provider.Id,
                Code = provider.Code,
                Name = provider.Name,
                Category = provider.Category.ToString(),
                LogoUrl = provider.LogoUrl,
                IsActive = provider.IsActive,
                SortOrder = provider.SortOrder
            };
        }

        // Mobil nömrə 055 123 45 67 kimi göstərilir, qalanı yazıldığı kimi
        public static string DisplayIdentifier(BillCategory category, string identifier)
        {
            if (category == BillCategory.Mobile && identifier.Length == 10)
            {
                return BillingEngine.FormatPhone(identifier);
            }
            return identifier;
        }

        // Mobil nömrənin sahibi maskalanır ("Elçin H."), kommunalda tam ad göstərilir
        private static string DisplayName(BillCategory category, string fullName)
        {
            if (category != BillCategory.Mobile)
            {
                return fullName;
            }
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return fullName;
            }
            return LoanMapper.MaskName(parts[0], parts[parts.Length - 1]);
        }

        public static BillLookupDto ToLookup(BillProvider provider, BillSubscriber subscriber, BillCharge? lastCharge)
        {
            var debt = subscriber.Balance < 0m ? -subscriber.Balance : 0m;
            return new BillLookupDto
            {
                ProviderId = provider.Id,
                ProviderName = provider.Name,
                Category = provider.Category.ToString(),
                LogoUrl = provider.LogoUrl,
                Identifier = DisplayIdentifier(provider.Category, subscriber.Identifier),
                SubscriberName = DisplayName(provider.Category, subscriber.FullName),
                Debt = debt,
                Credit = subscriber.Balance > 0m ? subscriber.Balance : 0m,
                SuggestedAmount = debt,
                MonthlyFee = subscriber.MonthlyFee,
                PackageName = subscriber.PackageName,
                LastChargePeriod = lastCharge?.Period,
                LastChargeAmount = lastCharge?.Amount,
                LastChargeUsage = lastCharge?.Usage,
                UnitName = provider.UnitName,
                MinPayment = provider.MinPayment,
                MaxPayment = provider.MaxPayment
            };
        }

        public static BillReceiptDto ToReceipt(BillResult result)
        {
            return new BillReceiptDto
            {
                Reference = result.Reference,
                ProviderName = result.ProviderName,
                Category = result.Category.ToString(),
                LogoUrl = result.LogoUrl,
                Identifier = DisplayIdentifier(result.Category, result.Identifier),
                SubscriberName = DisplayName(result.Category, result.SubscriberName),
                Amount = result.Amount,
                DebtCleared = result.DebtCleared,
                CreditAdded = result.CreditAdded,
                RemainingDebt = result.SubscriberBalanceAfter < 0m ? -result.SubscriberBalanceAfter : 0m,
                BalanceAfter = result.SubscriberBalanceAfter > 0m ? result.SubscriberBalanceAfter : 0m,
                CashbackEarned = result.CashbackEarned,
                CardLabel = LoanMapper.CardLabel(result.CardNumber),
                CardBalanceAfter = result.CardBalanceAfter,
                PaidAt = result.PaidAt
            };
        }

        public static BillPaymentDto ToPaymentDto(BillPayment payment)
        {
            return new BillPaymentDto
            {
                Id = payment.Id,
                PaidAt = payment.CreatedAt,
                ProviderName = payment.ProviderName,
                Category = payment.Provider.Category.ToString(),
                LogoUrl = payment.Provider.LogoUrl,
                Identifier = DisplayIdentifier(payment.Provider.Category, payment.Identifier),
                Amount = payment.Amount,
                CashbackEarned = payment.CashbackEarned,
                CardLabel = LoanMapper.CardLabel(payment.Card.CardNumber),
                Reference = payment.Reference
            };
        }
    }
}
