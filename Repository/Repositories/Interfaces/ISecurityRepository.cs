using Domain.Entities;

namespace Repository.Repositories.Interfaces
{
    public interface ISecurityRepository
    {
        // Siqnal yazılır. Eyni (Kind, Reference) artıq varsa yazılmır və false qaytarır
        Task<bool> TryAddAsync(SecurityAlert alert);

        // Müştərinin başqasına göndərdiyi köçürmələr (öz kartları arasında olanlar sayılmır): [fromUtc, indi)
        Task<int> CountOutgoingSinceAsync(string userId, DateTime fromUtc);
        Task<decimal> SumOutgoingSinceAsync(string userId, DateTime fromUtc);

        Task<(List<SecurityAlert> Items, int Total)> GetPagedAsync(SecurityAlertStatus? status, SecurityAlertSeverity? severity,
            SecurityAlertKind? kind, string? search, int page, int pageSize);

        Task<SecurityAlert?> GetAsync(int id);
        Task SaveAsync(SecurityAlert alert);

        // Açıq siqnalların sayı, onlardan yüksək olanlar, son 7 gündə baxılanlar
        Task<(int Open, int OpenHigh, int ResolvedWeek)> GetCountsAsync(DateTime weekAgoUtc);
    }
}
