using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface IRecurringTransferRepository
    {
        Task<List<RecurringTransfer>> GetByUserAsync(string userId);
        Task<int> CountByUserAsync(string userId);
        Task<RecurringTransfer?> GetForUserAsync(int id, string userId);
        Task AddAsync(RecurringTransfer item);
        Task SaveAsync(RecurringTransfer item);
        Task DeleteAsync(RecurringTransfer item);

        // Aktiv və bu ay hələ işlənməmiş köçürmələrin nömrələri (vaxtı gəlibmi, servis RecurringTransferRules ilə yoxlayır)
        Task<List<int>> GetDueIdsAsync(string period);

        Task<RecurringTransfer?> GetForRunAsync(int id);
    }
}
