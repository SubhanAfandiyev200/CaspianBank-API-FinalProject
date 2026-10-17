using Service.Helpers.DTOs.Staff;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    // İşçilərin idarəsi: yalnız SuperAdmin çağırır (controller-də məhdudlaşdırılıb). SuperAdmin hesabına və öz hesabına toxunmaq olmaz
    public interface IStaffService
    {
        Task<StaffListDto> GetListAsync();

        // Yeni işçi: hesab yaradılır, şifrəni özü seçsin deyə email göndərilir
        Task<ServiceResult<StaffDto>> AddAsync(AddStaffDto model);

        Task<ServiceResult<StaffDto>> ChangeRoleAsync(string actorUserId, string staffId, ChangeStaffRoleDto model);

        // Deaktiv: işçi daxil ola bilmir və açıq tokeni də dərhal etibarsız olur. Aktiv: yenidən daxil ola bilər
        Task<ServiceResult<StaffDto>> SetActiveAsync(string actorUserId, string actorName, string staffId, bool active);
    }
}
