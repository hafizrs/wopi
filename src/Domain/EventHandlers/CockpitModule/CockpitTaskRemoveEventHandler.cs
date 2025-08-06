using System;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.CockpitModule;

public class CockpitTaskRemoveEventHandler : IBaseEventHandlerAsync<GenericEvent>
{
    private readonly ILogger<CockpitTaskRemoveEventHandler> _logger;
    private readonly ICockpitTaskRemoveEventHandlerService _cockpitTaskRemoveEventHandlerService;

    public CockpitTaskRemoveEventHandler(
        ILogger<CockpitTaskRemoveEventHandler> logger,
        ICockpitTaskRemoveEventHandlerService cockpitTaskRemoveEventHandlerService)
    {
        _logger = logger;
        _cockpitTaskRemoveEventHandlerService = cockpitTaskRemoveEventHandlerService;
    }
    public async Task<bool> HandleAsync(GenericEvent @event)
    {
        _logger.LogInformation("Entered into event handler: {HandlerName} with   payload {Payload}.",
            nameof(CockpitTaskRemoveEventHandler), @event.JsonPayload);

        var response = true;
        try
        {
            await _cockpitTaskRemoveEventHandlerService.InitiateCockpitTaskRemoveEvent();
        }
        catch (Exception ex)
        {
            _logger.LogError("Exception occured during {HandlerName} event handle.", nameof(PraxisEventType.CockpitTaskRemoveEvent));
            _logger.LogError("Exception Message: {Message}  Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            response = false;
        }

        _logger.LogInformation("Handled by: {HandlerName}.", nameof(CockpitTaskRemoveEventHandler));
        return response;
    }
}