using Microsoft.Extensions.DependencyInjection;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumboard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumboard.CirsPermissions;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.Reports.PraxisUserReport;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CirsScrumboard.CirsPermissions;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Report.PraxisUserReport;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CirsScrumboard;

public static class CirsScrumboardServiceCollectionExtensions
{
    public static void AddCirsScrumboardServices(this IServiceCollection services)
    {
        services.AddSingleton<ICirsReportCreateService, CirsReportCreateService>();
        services.AddSingleton<ICirsReportQueryService, CirsReportQueryService>();
        services.AddSingleton<ICirsReportUpdateService, CirsReportUpdateService>();
        services.AddSingleton<IDeleteCirsReportsService, DeleteCirsReportsService>();
        services.AddSingleton<IActiveInactiveCirsReportService, ActiveInactiveCirsReportService>();
        services.AddSingleton<ICirsProcessGuideAttachmentService, CirsProcessGuideAttachmentService>();
        services.AddSingleton<ICirsOpenItemAttachmentService, CirsOpenItemAttachmentService>();
        services.AddSingleton<IGetCirsAdminsService, GetCirsAdminsService>();
        services.AddSingleton<ICirsPermissionService, CirsPermissionService>();
        services.AddSingleton<ICirsAdminAssignedEventHandlerService, CirsAdminAssignedEventHandlerService>();
        services.AddSingleton<ICirsDashboardUpdateService, CirsDashboardUpdateService>();
        services.AddSingleton<IPermissionsFactoryService, PermissionsFactoryService>();
        services.AddSingleton<IncidentPermissionsService>();
        services.AddSingleton<IdeaPermissionsService>();
        services.AddSingleton<AnotherPermissionsService>();
        services.AddSingleton<HintPermissionsService>();
        services.AddSingleton<ComplainPermissionsService>();
        services.AddSingleton<FaultPermissionService>();
        services.AddSingleton<ICirsRiskManagementAttachmentService, CirsRiskManagementAttachmentService>();
        services.AddSingleton<ICirsReportEventHandlerService, CirsReportEventHandlerService>();
        services.AddSingleton<ICirsReportGenerationService, CirsReportGenerationService>();
        services.AddSingleton<CirsReportEventHandler>();
    }
}
