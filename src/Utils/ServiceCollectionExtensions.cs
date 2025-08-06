using Microsoft.Extensions.DependencyInjection;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Linq;
using System.Reflection;

namespace Selise.Ecap.SC.WopiMonitor.Utils
{
    public static class ServiceCollectionExtensions
    {
        public static void RegisterAllDerivedTypes<T>(
            this IServiceCollection services,
            Assembly[] assemblies,
            ServiceLifetime lifetime = ServiceLifetime.Singleton
        )
        {
            var typesFromAssemblies = assemblies.SelectMany(
                a => a.DefinedTypes.Where(x => x.BaseType == typeof(T) || x.GetInterfaces().Contains(typeof(T)))
            );
            foreach (var type in typesFromAssemblies)
                services.Add(new ServiceDescriptor(typeof(T), type, lifetime));
        }

        public static void AddRegisterAllDerivedTypesServices(this IServiceCollection services)
        {
            var assemblies = new[]
            {
                typeof(ServiceCollectionExtensions).Assembly,
                typeof(ICommandHandler<,>).Assembly,
                typeof(IQueryHandler<,>).Assembly,
                typeof(IValidationHandler<,>).Assembly
            };

            services.RegisterAllDerivedTypes<ICommandHandler<,>>(assemblies);
            services.RegisterAllDerivedTypes<IQueryHandler<,>>(assemblies);
            services.RegisterAllDerivedTypes<IValidationHandler<,>>(assemblies);
        }

        public static void AddCommandValidator(this IServiceCollection services)
        {
            services.AddScoped<IValidationHandler<,>, ValidationHandler>();
        }
    }
} 