using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using static Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ReportConstants;
using SeliseBlocks.Genesis.Framework.Events;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.CirsScrumboard;

public class CirsAdminAssignedEventHandlerService : ICirsAdminAssignedEventHandlerService
{
    private readonly ILogger<CirsAdminAssignedEventHandlerService> _logger;
    private readonly IRepository _repository;
    private readonly IChangeLogService _changeLogService;
    private readonly ICirsPermissionService _cirsPermissionService;
    private readonly IServiceClient _serviceClient;

    public CirsAdminAssignedEventHandlerService(
        ILogger<CirsAdminAssignedEventHandlerService> logger,
        IRepository repository,
        IChangeLogService changeLogService,
        ICirsPermissionService cirsPermissionService,
        IServiceClient serviceClient
    )
    {
        _logger = logger;
        _repository = repository;
        _changeLogService = changeLogService;
        _cirsPermissionService = cirsPermissionService;
        _serviceClient = serviceClient;
    }

    public async Task<bool> InitiateAdminAssignedAfterEffectsAsync(string dashboardPermissionId)
    {
        var response = false;
        var dashboardPermission = await GetCirsDashboardPermissionAsync(dashboardPermissionId);

        if (dashboardPermission != null)
        {
            _logger.LogInformation(
                $"Data permission update started for collection name: {nameof(CirsDashboardPermission)} with organizationId:  {dashboardPermission.OrganizationId}.");

            response = await UpdateCirsReportsAsync(dashboardPermission);
            PublishUpdateCirsAssignedAdminForCockpitEvent(dashboardPermissionId);

            if (response)
            {
                _logger.LogInformation(
                    $"Successfully updated all required data permissions for collection name: {nameof(CirsGenericReport)} with organizationId:  {dashboardPermission.OrganizationId}.");
            }
        }
        else
        {
            _logger.LogInformation($"Operation aborted as no organization found with organizationId:  {dashboardPermission.OrganizationId}.");
        }
        return response;
    }

    private Task<CirsDashboardPermission> GetCirsDashboardPermissionAsync(string dashboardPermissionId)
    {
        return _repository.GetItemAsync<CirsDashboardPermission>(d => d.ItemId == dashboardPermissionId);
    }

    private async Task<bool> UpdateCirsReportsAsync(CirsDashboardPermission permission)
    {
        var reports = _repository.GetItems<CirsGenericReport>
            (c => permission.CirsDashboardName == c.CirsDashboardName && c.AffectedInvolvedParties != null && c.AffectedInvolvedParties.Any(a => a.PraxisClientId == permission.PraxisClientId)).ToList();

        var updateTasks = new List<Task>();

        foreach (var report in reports)
        {
            updateTasks.Add(UpdateCirsReportAsync(report));
        }

        await Task.WhenAll(updateTasks);

        return true;
    }

    private Task UpdateCirsReportAsync(CirsGenericReport report)
    {
        var cirsReportUpdates = PrepareCirsReportUpdates(report);

        var filter = Builders<BsonDocument>.Filter.Eq("_id", report.ItemId);

        return _changeLogService.UpdateChange(nameof(CirsGenericReport), filter, cirsReportUpdates);
    }


    private Dictionary<string, object> PrepareCirsReportUpdates(CirsGenericReport report)
    {
        _cirsPermissionService.SetCirsReportPermission(report);
        var idsAllowedToRead = report.IdsAllowedToRead;
        var cirsReportUpdates = new Dictionary<string, object>
        {
            { nameof(CirsGenericReport.IdsAllowedToRead), idsAllowedToRead },
            { nameof(CirsGenericReport.LastUpdateDate),  DateTime.UtcNow.ToLocalTime() }
        };

        return cirsReportUpdates;
    }
    private void PublishUpdateCirsAssignedAdminForCockpitEvent(string dashboardPermissionId)
    {
        if (string.IsNullOrEmpty(dashboardPermissionId))
        {
            _logger.LogWarning("Dashboard permission ID is null or empty. Cannot publish UpdateCirsAssignedAdminForCockpitEvent.");
            return;
        }
        var updateCirsAssignedAdminForCockpitEvent = new GenericEvent
        {
            EventType = PraxisEventType.UpdateCirsAssignedAdminForCockpitEvent,
            JsonPayload = JsonConvert.SerializeObject(new UpdateCirsAssignedAdminForCockpitEventModel
            {
                DashboardPermissionIds = new List<string> { dashboardPermissionId }
            })
        };

        _serviceClient.SendToQueue<GenericEvent>(PraxisConstants.GetPraxisQueueName(), updateCirsAssignedAdminForCockpitEvent);
    }
}