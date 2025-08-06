using Microsoft.Extensions.DependencyInjection;
using Selise.Ecap.SC.WopiMonitor.Contracts.DomainServices.WopiModule;
using Selise.Ecap.SC.WopiMonitor.Domain.DomainServices.WopiModule;
using Selise.Ecap.SC.WopiMonitor.Validators;

namespace Selise.Ecap.SC.WopiMonitor.WebService
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWopiModuleServices(this IServiceCollection services)
        {
            services.AddScoped<IWopiService, WopiService>();
            services.AddScoped<IWopiPermissionService, WopiPermissionService>();
            return services;
        }

        public static IServiceCollection AddWopiModuleCommandValidators(this IServiceCollection services)
        {
            services.AddScoped<CreateWopiSessionCommandValidator>();
            services.AddScoped<DeleteWopiSessionCommandValidator>();
            return services;
        }
    }
}