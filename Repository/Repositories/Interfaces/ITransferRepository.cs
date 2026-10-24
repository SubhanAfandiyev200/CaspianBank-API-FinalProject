using Repository.Results;

namespace Repository.Repositories.Interfaces
{
    public interface ITransferRepository
    {
        // Bir tranzaksiyada: göndərənin balansı azalır, alanın artır, jurnal qeydləri yazılır. Ya hamısı olur, ya heç biri.
        Task<TransferResult> ExecuteAsync(TransferPlan plan);

        // Bu istifadəçinin həmin Reference ilə artıq icra olunmuş köçürməsi (yoxdursa null)
        Task<TransferRecord?> FindAsync(string reference, string userId);

        // Başqa bankın kartına köçürmə: bir tranzaksiyada göndərənin balansı azalır (məbləğ + komissiya), jurnal və köçürmə qeydi yazılır
        Task<TransferResult> ExecuteExternalAsync(ExternalTransferPlan plan);

        // Bu istifadəçinin həmin Reference ilə artıq icra olunmuş başqa banka köçürməsi (yoxdursa null)
        Task<Domain.Entities.ExternalTransfer?> FindExternalAsync(string reference, string userId);

        // Kartdan [fromUtc, toUtc) aralığında göndərilmiş cəm (komissiya daxil deyil): aylıq limit hesabı üçün
        Task<decimal> SumSentAsync(int cardId, DateTime fromUtc, DateTime toUtc);
    }
}
