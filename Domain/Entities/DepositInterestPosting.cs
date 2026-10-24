using Domain.Common;

namespace Domain.Entities
{
    // Depozitin aylıq faiz yazılışı: hər ay bir sətir (müddət 6 aydırsa 6 sətir). CreatedAt faizin yazıldığı (düşdüyü) gündür.
    // Pul müddət bitəndə ödənilir, amma müştəri faizin ay-ay artdığını görür və bank hər ayın xərcini həmin ay yazır.
    // Sətirlərin cəmi həmişə Deposit.ExpectedInterest-ə bərabərdir (son ay yuvarlaqlaşdırma fərqini götürür)
    public class DepositInterestPosting : BaseEntity
    {
        public int DepositId { get; set; }
        public Deposit Deposit { get; set; } = null!;

        public int Number { get; set; }             // 1..TermMonths
        public decimal Amount { get; set; }
    }
}
