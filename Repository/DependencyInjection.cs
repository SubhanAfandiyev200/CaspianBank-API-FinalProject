using Microsoft.Extensions.DependencyInjection;
using Repository.Repositories;
using Repository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRepositoryLayer(this IServiceCollection services)
        {
            services.AddScoped<IBrandRepository, BrandRepository>();
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<IHomeTickerRepository, HomeTickerRepository>();
            services.AddScoped<IAboutRepository, AboutRepository>();
            services.AddScoped<IAboutPillarRepository, AboutPillarRepository>();
            services.AddScoped<IEmailOtpRepository, EmailOtpRepository>();
            services.AddScoped<IServiceSectionRepository, ServiceSectionRepository>();
            services.AddScoped<IServiceItemRepository, ServiceItemRepository>();
            services.AddScoped<IBenefitSectionRepository, BenefitSectionRepository>();
            services.AddScoped<IBenefitItemRepository, BenefitItemRepository>();
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            return services;
        }
    }
}
