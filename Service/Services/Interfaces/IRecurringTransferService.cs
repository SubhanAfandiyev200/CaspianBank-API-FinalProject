using Service.Helpers.DTOs.Transfers;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    // Aylıq təkrarlanan köçürmələr. UserId HƏMİŞƏ tokendən götürülür
    public interface IRecurringTransferService
    {
        Task<IEnumerable<RecurringTransferDto>> GetAsync(string userId);

        // Alıcı və kart yoxlanır (adi köçürmənin qaydaları ilə); məbləğ üçün balans indi yoxlanmır, hər ay köçürmə vaxtı yoxlanır
        Task<ServiceResult<RecurringTransferDto>> CreateAsync(string userId, CreateRecurringTransferDto model);

        Task<ServiceResult<RecurringTransferDto>> SetActiveAsync(string userId, int id, bool active);
        Task<ServiceResult<bool>> DeleteAsync(string userId, int id);

        // Arxa plan işi: vaxtı gələn köçürmələri edir. Edilənlərin sayı
        Task<int> RunDueAsync();
    }
}
