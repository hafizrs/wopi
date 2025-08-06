using Microsoft.Extensions.DependencyInjection;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services;
using Selise.Ecap.SC.PraxisMonitor.Domain.MongoDb;
using SeliseBlocks.MailService.Driver;
using System;
using System.Linq;
using System.Reflection;

public static class PraxisBusinessServiceCollectionExtensions
{
    public static void AddPraxisBusinessServices(this IServiceCollection services)
    {
        services.AddTransient<IMongoClientRepository, MongoClientRepository>();
        services.AddTransient<IMongoDataService, MongoDataService>();
        services.AddTransient<IMongoSecurityService, MongoSecurityService>();
        services.AddTransient<IMailServiceClient, MailServiceClient>();
        services.AddSingleton<IChangeLogService, ChangeLogService>();
        services.AddSingleton<IConnectionService, ConnectionService>();
        services.AddSingleton<IStorageDataService, StorageDataService>();
        services.AddSingleton<ICommonUtilService, CommonUtilService>();
        services.AddSingleton<ICreateDynamicLink, CreateDynamicLinkService>();
        services.AddSingleton<IAuthUtilityService, AuthUtilityService>();
        services.AddSingleton<ICreateDynamicLink, CreateDynamicLinkService>();
        services.AddSingleton<IPraxisFileService, PraxisFileService>();
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