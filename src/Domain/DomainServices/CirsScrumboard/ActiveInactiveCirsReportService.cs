using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CirsScrumboard;

#nullable enable
public class ActiveInactiveCirsReportService : IActiveInactiveCirsReportService
{
    private readonly IRepository _repository;
    private readonly ISecurityContextProvider _securityContextProvider;
    private readonly IChangeLogService _changeLogService;
    private readonly ICirsPermissionService _cirsPermissionService;
    private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;

    public ActiveInactiveCirsReportService(
        IRepository repository,
        ISecurityContextProvider securityContextProvider,
        IChangeLogService changeLogService,
        ICirsPermissionService cirsPermissionService,
        ICockpitSummaryCommandService cockpitSummaryCommandService)
    {
        _repository = repository;
        _securityContextProvider = securityContextProvider;
        _changeLogService = changeLogService;
        _cirsPermissionService = cirsPermissionService;
        _cockpitSummaryCommandService = cockpitSummaryCommandService;
    }

    public async Task InitiateActiveInactiveAsync(ActiveInactiveCirsReportCommand command)
    {
        var cirsReport = await GetCirsReportByIdAsync(command.CirsReportId);

        if (cirsReport != null &&
            cirsReport.IsActive != command.MarkAsActive &&
            IsValidStatusChangeLog(cirsReport.StatusChangeLog, command.MarkAsActive))
        {
            await UpdateCirsReports(cirsReport, command.MarkAsActive);
            await _cockpitSummaryCommandService.CreateSummary(cirsReport.ItemId,
                nameof(CockpitTypeNameEnum.CirsGenericReport), true);
        }
    } 

    private static bool IsValidStatusChangeLog(List<StatusChangeEvent> statusChangeLog, bool markAsActive)
    {
        var isValid = true;

        if (statusChangeLog != null && statusChangeLog.Count > 0)
        {
            var inactiveLog = statusChangeLog.FirstOrDefault(l => l.CurrentStatus == CirsCommonEnum.INACTIVE.ToString());
            isValid = markAsActive ? inactiveLog != null : inactiveLog == null;
        }

        return isValid;
    }

    private async Task<CirsGenericReport> GetCirsReportByIdAsync(string cirsReportId)
    {
        var securityContext = _securityContextProvider.GetSecurityContext();

        return
            await _repository
            .GetItemAsync<CirsGenericReport>(i =>
            i.ItemId == cirsReportId &&
            !i.IsMarkedToDelete);
    }

    private async Task<bool> UpdateCirsReports(CirsGenericReport cirsReport, bool markAsActive)
    {
        var updateTasks = new List<Task>();

        var filter = Builders<BsonDocument>.Filter.Eq("_id", cirsReport.ItemId);
        var cirsReportUpdates = PrepareActiveInactiveCirsReportUpdates(cirsReport, markAsActive);

        updateTasks.Add(
            _changeLogService.UpdateChange(
                nameof(CirsGenericReport),
                filter,
                cirsReportUpdates)
            );

        await Task.WhenAll(updateTasks);

        return true;
    }

    private Dictionary<string, object?> PrepareActiveInactiveCirsReportUpdates(
        CirsGenericReport cirsReport,
        bool markAsActive)
    {
        var securityContext = _securityContextProvider.GetSecurityContext();
        var currentDateTime = DateTime.UtcNow.ToLocalTime();

        var cirsReportUpdates = new Dictionary<string, object?>
        {
            {nameof(CirsGenericReport.IsActive), markAsActive},
            {nameof(CirsGenericReport.StatusChangeLog),  PrepareCirsReportStatusChangeLog(markAsActive, cirsReport, currentDateTime, securityContext.UserId)},
            {nameof(CirsGenericReport.LastUpdateDate),  DateTime.UtcNow.ToLocalTime()},
            {nameof(CirsGenericReport.LastUpdatedBy), securityContext.UserId},
        };

        return cirsReportUpdates;
    }

    private List<StatusChangeEvent> PrepareCirsReportStatusChangeLog(bool markAsActive, CirsGenericReport cirsReport, DateTime changedOn, string changedBy)
    {
        var statusChangeLog = cirsReport.StatusChangeLog ?? new List<StatusChangeEvent>();
        if (markAsActive)
        {
            statusChangeLog.RemoveAll(l => l.CurrentStatus == CirsCommonEnum.INACTIVE.ToString());
        }
        else
        {
            var statusChangeEvent = new StatusChangeEvent()
            {
                PreviousStatus = cirsReport.Status,
                CurrentStatus = CirsCommonEnum.INACTIVE.ToString(),
                ChangedOn = changedOn,
                ChangedBy = changedBy
            };
            statusChangeLog.Add(statusChangeEvent);
        }

        return statusChangeLog;
    }

    private string[] GetRolesAllowedToReadActiveCirsReport(CirsGenericReport cirsReport)
    {
        return IsAdminOnlyStatus(cirsReport)
            ? GetAllowedAdminRolesToReadCirsReport(cirsReport)
            : GetAllAllowedRolesToReadCirsReport(cirsReport);
    }

    private string[] GetIdsAllowedToReadActiveCirsReport(
        CirsGenericReport cirsReport,
        CirsDashboardPermission permission)
    {
        return IsAdminOnlyStatus(cirsReport)
            ? GetIdsAllowedToReadCirsReport(permission)
            : Array.Empty<string>();
    }

    private bool IsAdminOnlyStatus(CirsGenericReport cirsReport)
    {
        var enumValuesList = cirsReport.CirsDashboardName.GetCirsReportStatusEnumValues();
        return enumValuesList.Count > 3 && cirsReport.Status == enumValuesList[0];
    }

    private string[] GetAllowedAdminRolesToReadCirsReport(CirsGenericReport cirsReport)
    {
        var roles = new List<string>
        {
            RoleNames.Admin,
            RoleNames.TaskController,
            $"{RoleNames.AdminB_Dynamic}_{cirsReport.OrganizationId}"
        };

        return roles.ToArray();
    }

    private string[] GetAllAllowedRolesToReadCirsReport(CirsGenericReport cirsReport)
    {
        var roles = new List<string> {
            RoleNames.Admin,
            RoleNames.TaskController,
            $"{RoleNames.Organization_Read_Dynamic}_{cirsReport.OrganizationId}"
        };

        return roles.ToArray();
    }

    private static string[] GetIdsAllowedToReadCirsReport(CirsDashboardPermission? permission)
    {
        return permission?.AdminIds?.Select(ad => ad.UserId).ToArray() ?? Array.Empty<string>();
    }
}
