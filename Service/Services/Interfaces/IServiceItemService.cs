using Service.Helpers.DTOs.ServiceItems;

namespace Service.Services.Interfaces
{
    // Altı xidmət kartı hər biri proqramın bir bölməsinə aparır, ona görə admin yalnız mətnlərini dəyişir (əlavə/silmə yoxdur)
    public interface IServiceItemService
    {
        Task<IEnumerable<ServiceItemDto>> GetAllUIAsync();
        Task<IEnumerable<ServiceItemDto>> GetAllAsync();
        Task<ServiceItemDto> GetDetailAsync(int id);
        Task UpdateAsync(int id, ServiceItemUpdateDto model);
    }
}
