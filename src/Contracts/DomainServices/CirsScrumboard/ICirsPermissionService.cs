using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Client;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries.CirsReports;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;

#nullable enable
public interface ICirsPermissionService
{
    Task InitiateAssignCirsAdminsAsync(AssignCirsAdminsCommand command);

    Task<CirsDashboardPermission?> GetCirsDashboardPermissionAsync(
        string praxisClientId,
        CirsDashboardName dashboardName, bool returnDefaultValue = false);

    Task<CirsDashboardPermission?> GetCirsDashboardPermissionAsync(CirsGenericReport cirsGenericReport);

    Task<bool> IsACirsAdminAsync(string praxisClientId, CirsDashboardName dashboardName, PraxisClient? praxisClient = null, CirsDashboardPermission? dashboardPermission = null);
    Dictionary<string, bool> GetPermissionsByDashBoardName(CirsDashboardName dashboardName, CirsDashboardPermission dashboardPermissions);
    bool IsAAdminOrTaskController();
    AssignmentLevel? GetAssignmentLevelByDashboardName(CirsDashboardName dashboardName, CirsReportConfigModel? cirsReportConfig);
    void SetCirsReportPermission(CirsGenericReport report, CirsDashboardPermission? permission = null);
    Task<bool> HaveAllUnitViewPermissions(string userId, CirsDashboardName? dashboardName);
    bool checkDirectVisibilityPermission(Dictionary<string, bool> loggedInUserPermission, bool isActive, CirsDashboardName? dashboardName);
    Task<FilterDefinition<CirsGenericReport>> GetPermissionFilter(GetPermissionFilterModel model);
    List<string> PrepareRolesDisallowedToRead(CirsDashboardName dashboardName, ReportingVisibility? reportingVisibility, CirsDashboardPermission? dashboardPermission);
}