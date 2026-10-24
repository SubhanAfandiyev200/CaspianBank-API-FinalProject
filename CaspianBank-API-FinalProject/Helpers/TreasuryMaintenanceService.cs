using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Helpers
{
    // Arxa planda hər saat işləyir (proqram açıq olduqca): əvvəlki ay bağlanmayıbsa onun əməliyyat xərcini və mənfəət vergisini xəzinədən çıxış kimi yazır.
    // Proqram bağlı qalıbsa, buraxılmış aylar ilk açılışda bağlanır. Eyni ay ikinci dəfə bağlanmır (kodla yoxlanır)
    public class TreasuryMaintenanceService : BackgroundService
    {
        private static readonly TimeSpan FirstDelay = TimeSpan.FromSeconds(75);
        private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TreasuryMaintenanceService> _logger;

        public TreasuryMaintenanceService(IServiceScopeFactory scopeFactory, ILogger<TreasuryMaintenanceService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
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
                using var scope = _scopeFactory.CreateScope();
                var treasury = scope.ServiceProvider.GetRequiredService<ITreasuryService>();
                var closed = await treasury.CloseDueMonthsAsync();
                if (closed > 0)
                {
                    _logger.LogInformation("TreasuryMaintenance: {Count} ay bağlandı.", closed);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "TreasuryMaintenance işləmədi.");
            }
        }
    }
}
