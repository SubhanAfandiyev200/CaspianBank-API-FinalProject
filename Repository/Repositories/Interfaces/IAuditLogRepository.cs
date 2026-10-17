using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    // Audit qeydləri: yalnız əlavə etmək (AddAsync) və oxumaq. Dəyişdirmə və silmə metodu istifadə edilmir
    public interface IAuditLogRepository : IBaseRepository<AuditLog>
    {
        // search: işçi, hədəf və ya izah daxilində. action: dəqiq əməliyyat kodu. fromUtc/toUtc: [from, to) aralığı
        Task<(IReadOnlyList<AuditLog> Items, int Total)> GetPageAsync(string? search, string? action, DateTime? fromUtc, DateTime? toUtc, int skip, int take);
    }
}
