using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Domain.Builders.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CirsScrumboard;

#nullable enable
public class CirsDashboardUpdateService : ICirsDashboardUpdateService
{
    private readonly IRepository _repository;
    private readonly ISecurityContextProvider _securityContextProvider;
    private readonly IChangeLogService _changeLogService;
    private readonly ICirsPermissionService _cirsPermissionService;
    private readonly IEmailDataBuilder _emailDataBuilder;
    private readonly IEmailNotifierService _emailNotifierService;
    private readonly IReportingTaskCockpitSummaryCommandService _reportingTaskCockpitSummaryCommandService;

    public CirsDashboardUpdateService(
        IRepository repository,
        ISecurityContextProvider securityContextProvider,
        IChangeLogService changeLogService,
        ICirsPermissionService cirsPermissionService,
        IEmailDataBuilder emailDataBuilder,
        IEmailNotifierService emailNotifierService,
        IReportingTaskCockpitSummaryCommandService reportingTaskCockpitSummaryCommandService
    )
    {
        _repository = repository;
        _securityContextProvider = securityContextProvider;
        _changeLogService = changeLogService;
        _cirsPermissionService = cirsPermissionService;
        _emailDataBuilder = emailDataBuilder;
        _emailNotifierService = emailNotifierService;
        _reportingTaskCockpitSummaryCommandService = reportingTaskCockpitSummaryCommandService;
    }

    public async Task MoveToOtherDashboardAync(string cirsReportId, CirsDashboardName newDashboardName)
    {
        var cirsReport = await GetCirsReportByIdAsync(cirsReportId)
            ?? throw new InvalidOperationException($"CirsReport with ID: {cirsReportId} not found in the database");
        var statusInNewDashboard = GetStatusInNewBoard(newDashboardName, cirsReport);

        var updateTasks = new List<Task>();

        var cirsReportUpdates = await PrepareCirsDashboardChangedUpdatesAsync(
            cirsReport,
            newDashboardName,
            statusInNewDashboard);
        var filter = Builders<BsonDocument>.Filter.Eq("_id", cirsReport.ItemId);

        updateTasks.Add(
            _changeLogService.UpdateChange(nameof(CirsGenericReport), filter, cirsReportUpdates)
            );

        await Task.WhenAll(updateTasks);
        await ProcessEmailForResponsibleUsers(cirsReport, newDashboardName);
        await _reportingTaskCockpitSummaryCommandService.UpdateSummaryOnChangingDashboard(cirsReport.ItemId, newDashboardName);
    }

    private async Task ProcessEmailForResponsibleUsers(CirsGenericReport report, CirsDashboardName dashBoardName, string clientName = "")
    {
        if (report == null) return;
        var emailTasks = new List<Task<bool>>();
        var userIds = new List<string>();
        var purpose = string.Empty;

        if (dashBoardName == CirsDashboardName.Another)
        {
            purpose = EmailTemplateName.RequalifiedAsFeedback.ToString();
            if (!string.IsNullOrEmpty(report.ReportedBy?.UserId)) userIds.Add(report.ReportedBy.UserId);
            if (!string.IsNullOrEmpty(report.CreatedBy)) userIds.Add(report.CreatedBy);
        }

        if (userIds.Count > 0)
        {
            userIds = userIds.Distinct().ToList();
            var praxisUsers = _repository.GetItems<PraxisUser>(x => !x.IsMarkedToDelete && userIds.Contains(x.UserId)).ToList();
            foreach (var user in praxisUsers)
            {
                var person = new Person()
                {
                    DisplayName = user.FirstName,
                    Email = user.Email
                };
                var emailData = _emailDataBuilder.BuildCirsReportEmailData(report, person, clientName);
                var emailStatus = _emailNotifierService.SendEmail(
                                                            person.Email,
                                                            purpose,
                                                            emailData
                                                        );
                emailTasks.Add(emailStatus);
            }
        }

        if (emailTasks.Count > 0) await Task.WhenAll(emailTasks);
    }

    private static string GetStatusInNewBoard(CirsDashboardName newDashboardName, CirsGenericReport cirsReport)
    {
        var currentDashboardStatuses = cirsReport.CirsDashboardName.GetCirsReportStatusEnumValues();
        var newDashboardStatuses = newDashboardName.GetCirsReportStatusEnumValues();

        var currentStatusIndex = currentDashboardStatuses.IndexOf(cirsReport.Status);
        var newStatusIndex = currentStatusIndex;
        int statusDifference = newDashboardStatuses.Count - currentDashboardStatuses.Count;

        if (statusDifference != 0)
        {
            newStatusIndex = Math.Max(currentStatusIndex + statusDifference, 0);
        }

        var statusInNewDashboard = newDashboardStatuses[newStatusIndex];
        return statusInNewDashboard;
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

    private async Task<Dictionary<string, object?>> PrepareCirsDashboardChangedUpdatesAsync(
        CirsGenericReport cirsReport,
        CirsDashboardName newDashboardName,
        string statusInNewBoard)
    {
        var securityContext = _securityContextProvider.GetSecurityContext();
        var currentDateTime = DateTime.UtcNow.ToLocalTime();

        var clientId = cirsReport.AffectedInvolvedParties?.FirstOrDefault()?.PraxisClientId ?? string.Empty;

        cirsReport.CirsDashboardName = newDashboardName;
        cirsReport.Status = statusInNewBoard;
        cirsReport.StatusChangeLog = PrepareCirsReportStatusChangeLog(
                    cirsReport,
                    statusInNewBoard,
                    currentDateTime,
                    securityContext.UserId);
        cirsReport.Rank = GetNextRankValue(clientId, newDashboardName, statusInNewBoard);
        cirsReport.MetaData ??= new Dictionary<string, object?>();
        cirsReport.MetaData[$"{CommonCirsMetaKey.ReportingVisibility}"] = ReportingVisibility.Officer.ToString();

        var newDashboardPermission = await _cirsPermissionService.GetCirsDashboardPermissionAsync(cirsReport);
        _cirsPermissionService.SetCirsReportPermission(cirsReport, newDashboardPermission);

        var cirsReportUpdates = new Dictionary<string, object?>
        {
            { nameof(CirsGenericReport.CirsDashboardName), (int)newDashboardName },
            { nameof(CirsGenericReport.Status), statusInNewBoard },
            {
                nameof(CirsGenericReport.StatusChangeLog),
                cirsReport?.StatusChangeLog },
            {
                nameof(CirsGenericReport.RolesAllowedToRead),
                cirsReport?.RolesAllowedToRead
            },
            {
                nameof(CirsGenericReport.IdsAllowedToRead),
                cirsReport?.IdsAllowedToRead
            },
            { nameof(CirsGenericReport.LastUpdateDate),  DateTime.UtcNow.ToLocalTime() },
            { nameof(CirsGenericReport.LastUpdatedBy), securityContext.UserId },
            { nameof(CirsGenericReport.Rank), cirsReport?.Rank }
        };

        return cirsReportUpdates;
    }

    private ulong GetNextRankValue(string clientId, CirsDashboardName cirsDashboardName, string status)
    {
        ulong lastRank = _repository.GetItems<CirsGenericReport>
                (c => c.AffectedInvolvedParties != null && c.AffectedInvolvedParties.Any(a => a.PraxisClientId == clientId) &&
                c.CirsDashboardName == cirsDashboardName && c.Status == status)?.OrderByDescending(c => c.Rank)?.FirstOrDefault()?.Rank ?? 0;
        return lastRank + 1;
    }

    private List<StatusChangeEvent> PrepareCirsReportStatusChangeLog(
        CirsGenericReport cirsReport,
        string statusOnNewDashboard,
        DateTime changedOn,
        string changedBy)
    {
        var statusChangeLog = cirsReport.StatusChangeLog ?? new List<StatusChangeEvent>();

        var statusChangeEvent = new StatusChangeEvent()
        {
            PreviousStatus = cirsReport.Status,
            CurrentStatus = statusOnNewDashboard,
            ChangedOn = changedOn,
            ChangedBy = changedBy
        };
        statusChangeLog.Add(statusChangeEvent);

        return statusChangeLog;
    }

    private string[] GetRolesAllowedToReadActivedCirsReport(CirsGenericReport cirsReport)
    {
        return IsAdminOnlyStatus(cirsReport)
            ? GetAllowedAdminRolesToReadCirsReport(cirsReport)
            : GetAllAllowedRolesToReadCirsReport(cirsReport);
    }

    private string[] GetIdsAllowedToReadActivedCirsReport(
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
