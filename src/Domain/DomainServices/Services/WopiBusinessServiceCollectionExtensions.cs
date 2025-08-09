using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

public static class WopiBusinessServiceCollectionExtensions
{
    public static void AddWopiBusinessServices(this IServiceCollection services)
    {
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