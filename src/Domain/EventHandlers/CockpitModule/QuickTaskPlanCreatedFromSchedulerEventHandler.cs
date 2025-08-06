using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.QuickTaskModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.CockpitModule;

public class QuickTaskPlanCreatedFromSchedulerEventHandler : IBaseEventHandlerAsync<GenericEvent>
{
    private readonly ILogger<QuickTaskPlanCreatedFromSchedulerEventHandler> _logger;
    private readonly IQuickTaskPlanCreatedFromSchedulerEventHandlerService _quickTaskPlanCreatedFromSchedulerEventHandlerService;

    public QuickTaskPlanCreatedFromSchedulerEventHandler(
        ILogger<QuickTaskPlanCreatedFromSchedulerEventHandler> logger,
        IQuickTaskPlanCreatedFromSchedulerEventHandlerService quickTaskPlanCreatedFromSchedulerEventHandlerService)
    {
        _logger = logger;
        _quickTaskPlanCreatedFromSchedulerEventHandlerService = quickTaskPlanCreatedFromSchedulerEventHandlerService;
    }

    public async Task<bool> HandleAsync(GenericEvent @event)
    {
        _logger.LogInformation("Entered event handler: {HandlerName} with payload {Payload}.", nameof(QuickTaskPlanCreatedFromSchedulerEventHandler), @event.JsonPayload);
        try
        {
            var quickTaskPlanIds = JsonConvert.DeserializeObject<List<string>>(@event.JsonPayload);
            if (quickTaskPlanIds == null || quickTaskPlanIds.Count == 0)
            {
                _logger.LogWarning("No quick task plan ids found in the payload. Payload: {Payload}", @event.JsonPayload);
                return false;
            }
            await _quickTaskPlanCreatedFromSchedulerEventHandlerService.InitiateCockpitGenerationService(quickTaskPlanIds);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("Error occured while handling event: {HandlerName} with payload {Payload}. Error Message: {Message}.    Error Details: {StackTrace}", nameof(QuickTaskPlanCreatedFromSchedulerEventHandler), @event.JsonPayload, e.Message, e.StackTrace);
            return false;
        }
    }
} 