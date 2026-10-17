using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Helpers
{
    // Arxa planda hər saat işləyir (proqram açıq olduqca): müddəti bitmiş depozitlərin əsas məbləği və faizi karta qaytarılır.
    // Proqram bağlı qalıbsa, ilk açılışda keçmiş müddətlər də ödənilir. Eyni depozit iki dəfə ödənmir (vəziyyət və unikal jurnal qeydi)
    public class DepositMaintenanceService : BackgroundService
    {
        private static readonly TimeSpan FirstDelay = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DepositMaintenanceService> _logger;

        public DepositMaintenanceService(IServiceScopeFactory scopeFactory, ILogger<DepositMaintenanceService> logger)
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
                var deposits = scope.ServiceProvider.GetRequiredService<IDepositService>();
                var paid = await deposits.RunMaturityAsync();
                if (paid > 0)
                {
                    _logger.LogInformation("DepositMaintenance: {Count} depozitin müddəti bitdi və pulu qaytarıldı.", paid);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "DepositMaintenance işləmədi.");
            }
        }
    }
}
