using Service.Helpers.DTOs.History;

namespace Service.Services.Interfaces
{
    // Müştərinin əməliyyat tarixçəsi (bütün kartlar üzrə) və bir kartın çıxarışı
    public interface IHistoryService
    {
        Task<HistoryPageDto> GetHistoryAsync(string userId, HistoryFilterDto filter);
        Task<StatementDto> GetStatementAsync(string userId, StatementFilterDto filter);
    }
}
