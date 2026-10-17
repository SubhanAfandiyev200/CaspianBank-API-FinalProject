using Domain.Bills;
using Domain.Entities;
using Domain.Enums;
using Service.Helpers.DTOs.Bills;

namespace Service.Helpers
{
    // Təkrarlanan ödənişləri DTO-ya çevirir
    public static class RecurringMapper
    {
        public const string LoanName = "Caspian loan";

        // bakuToday: Bakı təqvim günü (vaxtsız)
        public static RecurringPaymentDto ToDto(RecurringPayment item, DateTime bakuToday, string currentPeriod)
        {
            var isLoan = item.ProviderId is null;
            var category = isLoan ? "Loan" : item.Provider!.Category.ToString();

            string status;
            if (!item.IsActive)
            {
                status = "Paused";
            }
            else if (!string.IsNullOrEmpty(item.LastError))
            {
                status = "Failed";
            }
            else
            {
                status = "Active";
            }

            // Mobil, kommunal, internet: ayın 1-i. Kredit: ayın son günü (taksitin vaxtı). Hazırdırsa və ya uğursuzdursa bu gün
            var nextRun = RecurringRules.NextRun(isLoan, bakuToday, item.LastPeriod);

            return new RecurringPaymentDto
            {
                Id = item.Id,
                ProviderId = item.ProviderId,
                ProviderName = isLoan ? LoanName : item.Provider!.Name,
                Category = category,
                LogoUrl = isLoan ? null : item.Provider!.LogoUrl,
                Identifier = isLoan ? item.Identifier : BillMapper.DisplayIdentifier(item.Provider!.Category, item.Identifier),
                Amount = item.Amount,
                CardLabel = LoanMapper.CardLabel(item.Card.CardNumber),
                IsActive = item.IsActive,
                Status = status,
                LastError = item.IsActive ? item.LastError : null,
                NextRunDate = nextRun
            };
        }
    }
}
