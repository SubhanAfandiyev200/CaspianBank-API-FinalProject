using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Service.Helpers.Settings;
using Service.Helpers.Validators.Accounts;
using Repository.Repositories;
using Repository.Repositories.Interfaces;
using Service.Services;
using Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServiceLayer(this IServiceCollection services)
        {
            services.AddScoped<IBrandService, BrandService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IOtpService, OtpService>();
            services.AddScoped<IPasswordService, PasswordService>();

            // SMTP tam qurulubsa (Host + istifadəçi + parol) MailKit, əks halda email mətni konsola yazılır
            services.AddScoped<IEmailSender>(sp =>
                sp.GetRequiredService<IOptions<SmtpSettings>>().Value.IsConfigured
                    ? ActivatorUtilities.CreateInstance<MailKitEmailSender>(sp)
                    : ActivatorUtilities.CreateInstance<LogEmailSender>(sp));
            services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();
            services.AddScoped<IHomeTickerService, HomeTickerService>();
            services.AddScoped<IAboutService, AboutService>();
            services.AddScoped<IAboutPillarService, AboutPillarService>();
            return services;
        }
    }
}
