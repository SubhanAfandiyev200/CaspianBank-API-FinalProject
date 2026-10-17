using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Helpers
{
    // Arxa planda hər saat işləyir (proqram açıq olduqca): vaxtı bitmiş kredit təkliflərini bağlayır,
    // gecikmiş aylıq ödənişlərə cərimə yazır və 90 gün gecikən kreditləri Court-a keçirir.
    // Cərimə oxunanda da "baxanda" hesablanır, ona görə bu xidmət dayansa belə rəqəmlər düzgün göstərilir;
    // xidmət isə vəziyyəti (Court, bağlanmış təklif) bazada yeniləyir.
    public class LoanMaintenanceService : BackgroundService
    {
        private static readonly TimeSpan FirstDelay = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LoanMaintenanceService> _logger;

        public LoanMaintenanceService(IServiceScopeFactory scopeFactory, ILogger<LoanMaintenanceService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Proqram açılanda baza və toxum işləri bitsin deyə bir az gözlənilir
                await Task.Delay(FirstDelay, stoppingToken);

                using var timer = new PeriodicTimer(Interval);
                do
                {
                    await RunOnceAsync(stoppingToken);
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
                // Proqram bağlanır
            }
        }

        private async Task RunOnceAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Hər işə yeni scope: DbContext və servislər scoped-dır
                using var scope = _scopeFactory.CreateScope();
                var loans = scope.ServiceProvider.GetRequiredService<ILoanService>();
                var changed = await loans.RunDailyMaintenanceAsync();
                if (changed > 0)
                {
                    _logger.LogInformation("LoanMaintenance: {Count} kredit yeniləndi.", changed);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Xəta proqramı dayandırmasın, növbəti saatda təkrar cəhd olunacaq
                _logger.LogError(ex, "LoanMaintenance işləmədi.");
            }
        }
    }
}
