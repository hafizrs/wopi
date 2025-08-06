using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.CockpitModule;

public class PraxisRiqsShiftPlanCreatedFromSchedulerEventHandler : IBaseEventHandlerAsync<GenericEvent>
{
    private readonly ILogger<PraxisRiqsShiftPlanCreatedFromSchedulerEventHandler> _logger;
    private readonly IPraxisRiqsShiftPlanCreatedFromSchedulerEventHandlerService _praxisRiqsShiftPlanCreatedFromSchedulerEventHandlerService;

    public PraxisRiqsShiftPlanCreatedFromSchedulerEventHandler(
        ILogger<PraxisRiqsShiftPlanCreatedFromSchedulerEventHandler> logger,
        IPraxisRiqsShiftPlanCreatedFromSchedulerEventHandlerService praxisRiqsShiftPlanCreatedFromSchedulerEventHandlerService)
    {
        _logger = logger;
        _praxisRiqsShiftPlanCreatedFromSchedulerEventHandlerService = praxisRiqsShiftPlanCreatedFromSchedulerEventHandlerService;
    }

    public async Task<bool> HandleAsync(GenericEvent @event)
    {
        _logger.LogInformation("Entered event handler: {HandlerName} with payload {Payload}.", nameof(PraxisRiqsShiftPlanCreatedFromSchedulerEventHandler), @event.JsonPayload);
        try
        {
            var shiftPlanIds = JsonConvert.DeserializeObject<List<string>>(@event.JsonPayload);
            if (shiftPlanIds == null || shiftPlanIds.Count == 0)
            {
                _logger.LogWarning("No shift plan ids found in the payload. Payload: {Payload}", @event.JsonPayload);
                return false;
            }
            await _praxisRiqsShiftPlanCreatedFromSchedulerEventHandlerService.InitiateCockpitGenerationService(shiftPlanIds);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("Error occured while handling event: {HandlerName} with payload {Payload}. Error Message: {Message}.    Error Details: {StackTrace}", nameof(PraxisRiqsShiftPlanCreatedFromSchedulerEventHandler), @event.JsonPayload, e.Message, e.StackTrace);
            return false;
        }
    }
}