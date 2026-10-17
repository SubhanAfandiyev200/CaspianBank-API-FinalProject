using Domain.Entities;
using Domain.Enums;
using Domain.Loans;
using Service.Helpers.DTOs.Loans;

namespace Service.Helpers
{
    // Kredit sətirlərini (izlənməyən surətlərini) müştərinin və işçinin gördüyü DTO-lara çevirir.
    // Cərimə burada "baxanda" hesablanır (LoanEngine.AccrueAll), amma izlənməyən surətdə olduğu üçün bazaya yazılmır
    public static class LoanMapper
    {
        // Müştəriyə göstərilən ümumi rədd mətni: işçinin yazdığı daxili səbəb müştəriyə çatmır
        public const string RejectedMessage = "We can't approve this application right now. You can apply again later.";

        public static string CardLabel(string cardNumber)
        {
            var last4 = cardNumber.Length >= 4 ? cardNumber.Substring(cardNumber.Length - 4) : cardNumber;
            return "•••• " + last4;
        }

        // "Elçin Hüseynov" -> "Elçin H."
        public static string MaskName(string name, string surname)
        {
            if (string.IsNullOrWhiteSpace(surname))
            {
                return name;
            }
            return name + " " + surname.Trim().Substring(0, 1) + ".";
        }

        private static bool IsOpen(Loan loan)
        {
            return loan.Status == LoanStatus.Active || loan.Status == LoanStatus.Court;
        }

        public static LoanSummaryDto ToSummary(Loan loan, DateTime today)
        {
            var summary = new LoanSummaryDto();
            FillSummary(summary, loan, today);
            return summary;
        }

        private static void FillSummary(LoanSummaryDto summary, Loan loan, DateTime today)
        {
            summary.Id = loan.Id;
            summary.ContractNumber = loan.ContractNumber;
            summary.Status = loan.Status.ToString();
            summary.Purpose = loan.Purpose.ToString();
            summary.RequestedAmount = loan.RequestedAmount;
            summary.ApprovedAmount = loan.ApprovedAmount;
            summary.TermMonths = loan.TermMonths;
            summary.MonthlyPayment = loan.MonthlyPayment;
            summary.OutstandingPrincipal = loan.OutstandingPrincipal;
            summary.AnnualInterestRate = loan.AnnualInterestRate;
            summary.CreatedAt = loan.CreatedAt;
            summary.DisbursedAt = loan.DisbursedAt;
            summary.OfferExpiresAt = loan.OfferExpiresAt;

            if (IsOpen(loan))
            {
                var due = LoanEngine.GetDue(loan, today);
                summary.NextDueDate = due.Items.Count > 0 ? due.Items[0].Installment.DueDate : null;
                summary.NextDueAmount = due.Total;
                summary.HasOverdue = due.HasOverdue;
            }

            switch (loan.Status)
            {
                case LoanStatus.Rejected:
                    summary.Message = RejectedMessage;
                    break;
                case LoanStatus.Declined:
                    summary.Message = loan.RejectReason;
                    break;
            }
        }

        public static LoanDetailDto ToDetail(Loan loan, DateTime today)
        {
            var detail = new LoanDetailDto();
            FillSummary(detail, loan, today);

            detail.PurposeNote = loan.PurposeNote;
            detail.TotalRepayment = loan.TotalRepayment;
            detail.InitialAnnualInterestRate = loan.InitialAnnualInterestRate;
            detail.TargetCardId = loan.TargetCardId;
            detail.TargetCardLabel = loan.TargetCard is null ? string.Empty : CardLabel(loan.TargetCard.CardNumber);
            detail.ClosedAt = loan.ClosedAt;
            detail.ClosedEarly = loan.ClosedEarly;
            detail.FixedPenalty = loan.FixedPenalty;
            detail.DailyPenaltyPercent = loan.DailyPenaltyPercent;

            if (loan.OwnBank is not null)
            {
                detail.MinEarlyPayment = loan.OwnBank.MinEarlyPayment;
                detail.EarlyRepaymentMinPercent = loan.OwnBank.EarlyRepaymentMinPercent;
                detail.EarlyRepaymentRateReduction = loan.OwnBank.EarlyRepaymentRateReduction;
                detail.MinAnnualInterestRate = loan.OwnBank.MinAnnualInterestRate;
            }

            foreach (var installment in loan.Installments.OrderBy(i => i.Number))
            {
                detail.Installments.Add(ToInstallment(installment, today));
            }

            foreach (var record in loan.PaymentRecords.OrderByDescending(r => r.PaidAt).ThenByDescending(r => r.Id))
            {
                detail.Payments.Add(new LoanPaymentDto
                {
                    PaidAt = record.PaidAt,
                    Type = record.Type.ToString(),
                    Amount = record.Amount,
                    Penalty = record.PenaltyPart,
                    Interest = record.InterestPart,
                    Principal = record.PrincipalPart,
                    InterestRateAfter = record.InterestRateAfter,
                    CardLabel = CardLabel(record.CardNumber),
                    Reference = record.TransactionReference
                });
            }

            if (IsOpen(loan))
            {
                var due = LoanEngine.GetDue(loan, today);
                detail.Due = new LoanDueDto
                {
                    Total = due.Total,
                    Penalty = due.PenaltyTotal,
                    Interest = due.InterestTotal,
                    Principal = due.PrincipalTotal,
                    OverdueTotal = due.OverdueTotal,
                    OverdueCount = due.OverdueCount,
                    InstallmentCount = due.Items.Count,
                    NextDueDate = due.Items.Count > 0 ? due.Items[0].Installment.DueDate : null
                };
                detail.CanPayInstallment = due.Total > 0m;

                detail.PayoffAmount = LoanEngine.GetPayoff(loan, today);
                detail.CanPayOff = detail.PayoffAmount > 0m;

                if (loan.Status != LoanStatus.Active)
                {
                    detail.EarlyBlockedReason = "A loan in court can only be repaid in full.";
                }
                else if (due.HasOverdue)
                {
                    detail.EarlyBlockedReason = "Pay the overdue installments first.";
                }
                detail.CanPartialEarly = detail.EarlyBlockedReason is null && detail.PayoffAmount > detail.MinEarlyPayment;
            }

            return detail;
        }

        private static LoanInstallmentDto ToInstallment(LoanInstallment installment, DateTime today)
        {
            string status;
            var daysOverdue = 0;
            if (installment.IsCancelled)
            {
                status = "Prepaid";
            }
            else if (LoanEngine.IsSettled(installment))
            {
                status = "Paid";
            }
            else if (installment.DueDate.Date < today.Date)
            {
                status = "Overdue";
                daysOverdue = (today.Date - installment.DueDate.Date).Days;
            }
            else
            {
                status = "Upcoming";
            }

            var remaining = installment.IsCancelled
                ? 0m
                : LoanEngine.InterestLeft(installment) + LoanEngine.PrincipalLeft(installment) + LoanEngine.PenaltyLeft(installment);

            return new LoanInstallmentDto
            {
                Number = installment.Number,
                DueDate = installment.DueDate,
                Principal = installment.PrincipalAmount,
                Interest = installment.InterestAmount,
                Payment = installment.PrincipalAmount + installment.InterestAmount,
                Penalty = installment.FixedPenaltyAmount + installment.AccruedPenalty,
                Paid = installment.PaidAmount + installment.PaidPenalty,
                Remaining = LoanEngine.Round(remaining),
                Status = status,
                DaysOverdue = daysOverdue
            };
        }

        public static LoanReceiptDto ToReceipt(Repository.Results.LoanResult result)
        {
            return new LoanReceiptDto
            {
                Reference = result.Reference,
                ContractNumber = result.ContractNumber,
                Type = result.Type.ToString(),
                Amount = result.Amount,
                Penalty = result.PenaltyPart,
                Interest = result.InterestPart,
                Principal = result.PrincipalPart,
                CardLabel = CardLabel(result.CardNumber),
                CardBalanceAfter = result.CardBalanceAfter,
                LoanClosed = result.LoanClosed,
                NewInterestRate = result.NewInterestRate,
                PaidAt = result.PaidAt
            };
        }
    }
}
