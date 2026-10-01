using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
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
            services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();
            services.AddScoped<IHomeTickerService, HomeTickerService>();
            services.AddScoped<IAboutService, AboutService>();
            services.AddScoped<IAboutPillarService, AboutPillarService>();
            return services;
        }
    }
}
