using Service.Helpers.DTOs.Audit;
using Service.Helpers.Responses;

namespace Service.Services.Interfaces
{
    public interface IAuditService
    {
        // Qeyd yazır. Xəta olsa əsas əməliyyatı pozmur (loglanır). İşçinin adı və rolu id-dən tapılır və qeydə surət kimi yazılır
        Task LogAsync(string? actorUserId, string action, string targetType, string targetLabel, string? details = null);

        Task<ServiceResult<AuditPageDto>> GetPageAsync(AuditFilterDto filter);
    }
}
