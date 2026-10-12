using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface ICustomerRepository
    {
        // Reyestrdə FİN və vəsiqə nömrəsi eyni vətəndaşa aiddirsə tapılır (ikisindən biri səhvdirsə null)
        Task<Customer?> GetByFinAndIdCardAsync(string fin, string idCardNumber);

        Task<Customer?> GetByIdAsync(int id);
    }
}
