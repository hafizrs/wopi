using Microsoft.Extensions.DependencyInjection;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsInterface;
namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CirsScrumboard;

public static class RiqsInterfaceServiceCollectionExtensions
{
    public static void AddRiqsInterfaceServices(this IServiceCollection services)
    {
        services.AddSingleton<IRiqsInterfaceDMSMigrationService, RiqsInterfaceDMSMigrationService>();
        services.AddSingleton<ISharePointFileAndFolderInfoService, SharePointFileAndFolderInfoService>();
        services.AddSingleton<IRiqsInterfaceManagerService, RiqsInterfaceManagerService>();
        services.AddSingleton<IRiqsInterfaceManagerLoginFlowService, RiqsInterfaceManagerLoginFlowService>();
        services.AddSingleton<IRiqsInterfaceTokenService, RiqsInterfaceTokenService>();
        services.AddSingleton<IRiqsInterfaceSessionService, RiqsInterfaceSessionService>();
        services.AddSingleton<IRiqsInterfaceSharePointMigrationService, RiqsInterfaceSharePointMigrationService>();
        services.AddSingleton<IRiqsInterfaceEquipmentMigrationService, RiqsInterfaceEquipmentMigrationService>();
        services.AddSingleton<IRiqsInterfaceGoogleDriveMigrationService, RiqsInterfaceGoogleDriveMigrationService>();
        services.AddSingleton<IGoogleDriveFileAndFolderInfoService, GoogleDriveFileAndFolderInfoService>();
        services.AddSingleton<IRiqsInterfaceProcessMigrationService, RiqsInterfaceProcessMigrationService>();
        services.AddSingleton<IRiqsInterfaceEquipmentService, RiqsInterfaceEquipmentService>();
        services.AddSingleton<IRiqsInterfaceConfigurationService, RiqsInterfaceConfigurationService>();
    }
}
