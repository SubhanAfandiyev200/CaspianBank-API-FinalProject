using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Service.Helpers.Settings;
using Service.Helpers.Validators.Accounts;
using Service.Services;
using Service.Services.Interfaces;

namespace Service
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServiceLayer(this IServiceCollection services)
        {
            services.AddScoped<IBrandService, BrandService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IOtpService, OtpService>();
            services.AddScoped<IPasswordService, PasswordService>();
            services.AddScoped<ICardDesignService, CardDesignService>();
            services.AddScoped<ICardHeroService, CardHeroService>();
            services.AddScoped<ISettingService, SettingService>();
            services.AddScoped<ICardService, CardService>();
            services.AddScoped<ICardTierService, CardTierService>();
            services.AddScoped<ITransferService, TransferService>();
            services.AddScoped<IHistoryService, HistoryService>();

            // SMTP tam qurulubsa (Host + istifadəçi + parol) MailKit, əks halda email mətni konsola yazılır
            services.AddScoped<IEmailSender>(sp =>
                sp.GetRequiredService<IOptions<SmtpSettings>>().Value.IsConfigured
                    ? ActivatorUtilities.CreateInstance<MailKitEmailSender>(sp)
                    : ActivatorUtilities.CreateInstance<LogEmailSender>(sp));
            services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();
            services.AddScoped<IHomeTickerService, HomeTickerService>();
            services.AddScoped<IHomeStatsService, HomeStatsService>();
            services.AddScoped<INotificationService, NotificationService>();
            // Canlı kanal API layihəsində SignalR ilə əvəzlənir; qurulmayıbsa bildiriş yalnız bazaya yazılır
            services.TryAddScoped<INotificationPublisher, NullNotificationPublisher>();
            services.AddScoped<IBillService, BillService>();
            services.AddScoped<IBillProviderService, BillProviderService>();
            services.AddScoped<IRecurringPaymentService, RecurringPaymentService>();
            services.AddScoped<IUserAdminService, UserAdminService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<ISupportTicketService, SupportTicketService>();
            services.AddScoped<ILoanService, LoanService>();
            services.AddScoped<ILoanReviewService, LoanReviewService>();
            services.AddScoped<IAboutService, AboutService>();
            services.AddScoped<IAboutPillarService, AboutPillarService>();
            services.AddScoped<IServiceSectionService, ServiceSectionService>();
            services.AddScoped<IServiceItemService, ServiceItemService>();
            services.AddScoped<IBenefitSectionService, BenefitSectionService>();
            services.AddScoped<IBenefitItemService, BenefitItemService>();
            return services;
        }
    }
}
