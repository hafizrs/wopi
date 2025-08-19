using Microsoft.Extensions.DependencyInjection;
using Selise.Ecap.SC.Wopi.Contracts.DomainServices.WopiModule;
using Selise.Ecap.SC.Wopi.Domain.DomainServices.WopiModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Linq;
using System.Reflection;

public static class WopiBusinessServiceCollectionExtensions
{
    public static void AddWopiBusinessServices(this IServiceCollection services)
    {
        services.AddSingleton<IWopiService, WopiService>();
        services.AddHostedService<WopiSessionCleanupService>();
    }

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
}