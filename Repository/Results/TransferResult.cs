namespace Repository.Results
{
    public enum TransferOutcome
    {
        Success,
        SourceNotFound,
        DestinationNotFound,
        SourceBlocked,
        DestinationBlocked,
        NotAllowed,         // Cashback qaydaları pozulur
        InsufficientFunds,
        Duplicate,          // eyni Reference artıq işlənib (forma iki dəfə göndərilib)
        Conflict            // eyni anda başqa əməliyyat balansı dəyişdi
    }

    public record TransferResult(TransferOutcome Outcome, decimal SourceBalanceAfter = 0m);

    // Servisin hesabladığı və yoxladığı köçürmə planı: repository yalnız onu atomik icra edir
    public record TransferPlan(
        string UserId,
        int FromCardId,
        int ToCardId,
        decimal Amount,
        decimal Commission,
        string Reference,
        string? Note,
        string DescriptionOut,
        string DescriptionIn);
}

namespace Repository.Results
{
    // Əvvəl icra olunmuş köçürmə (eyni Reference ikinci dəfə gələndə qəbz yenidən qurulur)
    public record TransferRecord(
        Domain.Entities.Card Source,
        Domain.Entities.Card Destination,
        decimal Amount,
        decimal Commission,
        string? Note,
        decimal SourceBalanceAfter,
        DateTime CreatedAt);
}
