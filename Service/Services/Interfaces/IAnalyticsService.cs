using Service.Helpers.DTOs.Analytics;

namespace Service.Services.Interfaces
{
    // Müştərinin xərc analitikası. UserId HƏMİŞƏ tokendən götürülür
    public interface IAnalyticsService
    {
        // Son N ayda (1-12) kartlardan çıxan pul kateqoriyalar üzrə
        Task<SpendingAnalyticsDto> GetSpendingAsync(string userId, int months);
    }
}
