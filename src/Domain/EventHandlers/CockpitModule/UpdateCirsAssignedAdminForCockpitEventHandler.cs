using System;
using System.Collections.Generic;
using System.Linq;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.CockpitModule;

public class UpdateCirsAssignedAdminForCockpitEventHandler : IBaseEventHandlerAsync<GenericEvent>
{
    private readonly ILogger<UpdateCirsAssignedAdminForCockpitEventHandler> _logger;
    private readonly ICockpitSummaryCommandService _cockpitSummaryCommandService;

    public UpdateCirsAssignedAdminForCockpitEventHandler(
        ILogger<UpdateCirsAssignedAdminForCockpitEventHandler> logger,
        ICockpitSummaryCommandService cockpitSummaryCommandService)
    {
        _logger = logger;
        _cockpitSummaryCommandService = cockpitSummaryCommandService;
    }
    public async Task<bool> HandleAsync(GenericEvent @event)
    {
        _logger.LogInformation("Entered into event handler: {HandlerName} with   payload {Payload}.",
            nameof(UpdateCirsAssignedAdminForCockpitEventHandler), JsonConvert.SerializeObject(@event.JsonPayload));

        var response = true;
        try
        {
            var eventModel = JsonConvert.DeserializeObject<UpdateCirsAssignedAdminForCockpitEventModel>(@event.JsonPayload);

            if (eventModel != null && eventModel.DashboardPermissionIds?.Count > 0)
            {
                var taskList = eventModel.DashboardPermissionIds.Select(async dashboardPermissionId =>
                {
                    _logger.LogInformation("Processing Dashboard Permission ID: {DashboardPermissionId} in {HandlerName}.", dashboardPermissionId, nameof(UpdateCirsAssignedAdminForCockpitEventHandler));
                    await _cockpitSummaryCommandService.UpdateCirsAssignedAdmins(dashboardPermissionId);
                }).ToList();

                await Task.WhenAll(taskList);
            }
            else
            {
                _logger.LogInformation("Operation aborted in {HandlerName} as payload is empty.", nameof(UpdateCirsAssignedAdminForCockpitEventHandler));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Exception occured during {HandlerName} event handle.", nameof(PraxisEventType.CirsAdminAssignedEvent));
            _logger.LogError("Exception Message: {Message}  Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            response = false;
        }

        _logger.LogInformation("Handled by: {HandlerName}.", nameof(UpdateCirsAssignedAdminForCockpitEventHandler));

        return response;
    }
}