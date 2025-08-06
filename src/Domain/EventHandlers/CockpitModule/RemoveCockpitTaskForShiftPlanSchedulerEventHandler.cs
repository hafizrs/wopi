using System;
using System.Collections.Generic;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.CockpitModule;

public class RemoveCockpitTaskForShiftPlanSchedulerEventHandler : IBaseEventHandlerAsync<GenericEvent>
{
    private readonly ILogger<RemoveCockpitTaskForShiftPlanSchedulerEventHandler> _logger;
    private readonly IRemoveCockpitTaskForShiftPlanSchedulerEventHandlerService _removeCockpitTaskForShiftPlanSchedulerEventHandlerService;

    public RemoveCockpitTaskForShiftPlanSchedulerEventHandler(ILogger<RemoveCockpitTaskForShiftPlanSchedulerEventHandler> logger, IRemoveCockpitTaskForShiftPlanSchedulerEventHandlerService removeCockpitTaskForShiftPlanSchedulerEventHandlerService)
    {
        _logger = logger;
        _removeCockpitTaskForShiftPlanSchedulerEventHandlerService = removeCockpitTaskForShiftPlanSchedulerEventHandlerService;
    }
    public async Task<bool> HandleAsync(GenericEvent @event)
    {
        _logger.LogInformation("Entered event handler: {HandlerName} with payload {Payload}.", nameof(RemoveCockpitTaskForShiftPlanSchedulerEventHandler), @event.JsonPayload);
        try
        {
            var shiftPlanIds = JsonConvert.DeserializeObject<List<string>>(@event.JsonPayload);
            if (shiftPlanIds == null || shiftPlanIds.Count == 0)
            {
                _logger.LogWarning("No shift plan ids found in the payload. Payload: {Payload}", @event.JsonPayload);
                return false;
            }
            await _removeCockpitTaskForShiftPlanSchedulerEventHandlerService.InitiateCockpitRevokedService(shiftPlanIds);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("Error occured while handling event: {HandlerName} with payload {Payload}. Error Message: {Message}.    Error Details: {StackTrace}", nameof(RemoveCockpitTaskForShiftPlanSchedulerEventHandler), @event.JsonPayload, e.Message, e.StackTrace);
            return false;
        }
    }
}