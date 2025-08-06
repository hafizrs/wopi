using System;
using System.Collections.Generic;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CockpitModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.CockpitModule;

public class RemoveCockpitTaskForQuickTaskPlanSchedulerEventHandler : IBaseEventHandlerAsync<GenericEvent>
{
    private readonly ILogger<RemoveCockpitTaskForQuickTaskPlanSchedulerEventHandler> _logger;
    private readonly IRemoveCockpitTaskForQuickTaskPlanSchedulerEventHandlerService _removeCockpitTaskForQuickTaskPlanSchedulerEventHandlerService;

    public RemoveCockpitTaskForQuickTaskPlanSchedulerEventHandler(ILogger<RemoveCockpitTaskForQuickTaskPlanSchedulerEventHandler> logger, IRemoveCockpitTaskForQuickTaskPlanSchedulerEventHandlerService removeCockpitTaskForQuickTaskPlanSchedulerEventHandlerService)
    {
        _logger = logger;
        _removeCockpitTaskForQuickTaskPlanSchedulerEventHandlerService = removeCockpitTaskForQuickTaskPlanSchedulerEventHandlerService;
    }
    public async Task<bool> HandleAsync(GenericEvent @event)
    {
        _logger.LogInformation("Entered event handler: {HandlerName} with payload {Payload}.", nameof(RemoveCockpitTaskForQuickTaskPlanSchedulerEventHandler), @event.JsonPayload);
        try
        {
            var quickTaskPlanIds = JsonConvert.DeserializeObject<List<string>>(@event.JsonPayload);
            if (quickTaskPlanIds == null || quickTaskPlanIds.Count == 0)
            {
                _logger.LogWarning("No quick task plan ids found in the payload. Payload: {Payload}", @event.JsonPayload);
                return false;
            }
            await _removeCockpitTaskForQuickTaskPlanSchedulerEventHandlerService.InitiateCockpitRevokedService(quickTaskPlanIds);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("Error occured while handling event: {HandlerName} with payload {Payload}. Error Message: {Message}.    Error Details: {StackTrace}", nameof(RemoveCockpitTaskForQuickTaskPlanSchedulerEventHandler), @event.JsonPayload, e.Message, e.StackTrace);
            return false;
        }
    }
} 