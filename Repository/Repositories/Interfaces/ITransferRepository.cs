using Repository.Results;

namespace Repository.Repositories.Interfaces
{
    public interface ITransferRepository
    {
        // Bir tranzaksiyada: göndərənin balansı azalır, alanın artır, jurnal qeydləri yazılır. Ya hamısı olur, ya heç biri.
        Task<TransferResult> ExecuteAsync(TransferPlan plan);

        // Bu istifadəçinin həmin Reference ilə artıq icra olunmuş köçürməsi (yoxdursa null)
        Task<TransferRecord?> FindAsync(string reference, string userId);
    }
}
