using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface ICustomerRepository
    {
        // Reyestrdə FİN və vəsiqə nömrəsi eyni vətəndaşa aiddirsə tapılır (ikisindən biri səhvdirsə null)
        Task<Customer?> GetByFinAndIdCardAsync(string fin, string idCardNumber);

        Task<Customer?> GetByIdAsync(int id);

        // Kredit baxışı üçün: iş yerləri (maaşla) və başqa banklardakı kreditləri ilə birlikdə
        Task<Customer?> GetWithHistoryAsync(int id);
    }
}
