using Service.Services.Interfaces;

namespace CaspianBank_API_FinalProject.Helpers
{
    // Arxa planda hər saat işləyir (proqram açıq olduqca): internet, işıq, qaz və su abunəçilərinə bu ay üçün hesab yazır.
    // Hesab hər abunəçiyə ayda bir dəfə yazılır (təkrar işləsə də ikinci dəfə yazılmır). Pul çatmasa abunəçinin balansı mənfiyə düşür, yəni borc yaranır.
    // Sonra müştərilərin aylıq təkrarlanan ödənişləri (hər ayın 1-i) edilir
    public class BillingMaintenanceService : BackgroundService
    {
        private static readonly TimeSpan FirstDelay = TimeSpan.FromSeconds(45);
        private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BillingMaintenanceService> _logger;

        public BillingMaintenanceService(IServiceScopeFactory scopeFactory, ILogger<BillingMaintenanceService> logger)
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
                var bills = scope.ServiceProvider.GetRequiredService<IBillService>();
                var written = await bills.RunMonthlyBillingAsync();
                if (written > 0)
                {
                    _logger.LogInformation("BillingMaintenance: {Count} abunəçiyə aylıq hesab yazıldı.", written);
                }

                // Aylıq hesab yazıldıqdan sonra müştərilərin təkrarlanan ödənişləri edilir (ayın 1-i; bağlı qalıbsa ilk açılışda)
                var recurring = scope.ServiceProvider.GetRequiredService<IRecurringPaymentService>();
                var paid = await recurring.RunDueAsync();
                if (paid > 0)
                {
                    _logger.LogInformation("BillingMaintenance: {Count} təkrarlanan ödəniş edildi.", paid);
                }

                // Müştərilərin aylıq təkrarlanan köçürmələri (seçilmiş gün)
                var transfers = scope.ServiceProvider.GetRequiredService<IRecurringTransferService>();
                var sent = await transfers.RunDueAsync();
                if (sent > 0)
                {
                    _logger.LogInformation("BillingMaintenance: {Count} təkrarlanan köçürmə edildi.", sent);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Xəta proqramı dayandırmasın, növbəti saatda təkrar cəhd olunacaq
                _logger.LogError(ex, "BillingMaintenance işləmədi.");
            }
        }
    }
}
