namespace Domain.Constants
{
    // Audit qeydlərinin əməliyyat kodları. Kodlar bazaya yazılır, ona görə adlarını dəyişmə
    public static class AuditActions
    {
        public const string AccountRestricted = "AccountRestricted";
        public const string AccountRestored = "AccountRestored";
        public const string LoanApproved = "LoanApproved";
        public const string LoanOffered = "LoanOffered";
        public const string LoanRejected = "LoanRejected";
        public const string BillProviderChanged = "BillProviderChanged";
        public const string SettingChanged = "SettingChanged";
        public const string TicketAnswered = "TicketAnswered";
        public const string TicketClosed = "TicketClosed";

        public static readonly string[] All =
        {
            AccountRestricted, AccountRestored, LoanApproved, LoanOffered, LoanRejected, BillProviderChanged, SettingChanged, TicketAnswered, TicketClosed
        };
    }
}
