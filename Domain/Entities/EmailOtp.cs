using Domain.Common;

namespace Domain.Entities
{
    public class EmailOtp : BaseEntity
    {
        public string Email { get; set; } = string.Empty;
        public string CodeHash { get; set; } = string.Empty;   // kod düz saxlanmır, HMAC hash-i saxlanır
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
        public int Attempts { get; set; }

        // Kod düzgün daxil ediləndə yaranır: Register bu tokenin hash-ini tələb edir
        public string? VerificationTokenHash { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }
}
