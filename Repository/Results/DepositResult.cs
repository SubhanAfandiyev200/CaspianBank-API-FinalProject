using Domain.Entities;

namespace Repository.Results
{
    public enum DepositOutcome
    {
        Success,
        PlanNotFound,
        PlanInactive,
        AmountOutOfRange,
        SourceNotFound,
        SourceBlocked,
        PayoutNotAllowed,
        InsufficientFunds,
        Duplicate,
        Conflict
    }

    // Depozit açmaq üçün servisin hazırladığı plan: repository onu təkrar yoxlayıb bir tranzaksiyada icra edir
    public record DepositOpenPlan(
        string UserId,
        int PlanId,
        decimal Amount,
        int SourceCardId,
        int PayoutCardId,
        string Reference,
        DateTime UtcNow);

    public class DepositOpenResult
    {
        public DepositOutcome Outcome { get; set; }
        public Deposit? Deposit { get; set; }
        public decimal SourceBalanceAfter { get; set; }

        public DepositOpenResult(DepositOutcome outcome)
        {
            Outcome = outcome;
        }
    }

    // Müddəti bitib ödənilmiş depozit (servis müştəriyə bildiriş yazır)
    public record MaturedDeposit(string UserId, int DepositId, decimal Principal, decimal Interest, string CardLast4, string Reference);

    // Admin üçün cəmlər
    public class DepositTotals
    {
        public int ActiveCount { get; set; }
        public decimal ActivePrincipal { get; set; }
        public decimal ExpectedInterest { get; set; }       // aktiv depozitlərin müddət sonunda veriləcək faizinin cəmi
        public decimal PaidInterest { get; set; }           // indiyə qədər ödənilmiş faiz
    }
}
