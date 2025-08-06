using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.LibraryModule;

public class DmsArtifactUsageReferenceDeletionEventHandler : IBaseEventHandlerAsync<GenericEvent>
{
    private readonly ILogger<DmsArtifactUsageReferenceDeletionEventHandler> _logger;
    private readonly IDmsArtifactUsageReferenceEventHandlerService _dmsArtifactUsageReferenceEventHandlerService;

    public DmsArtifactUsageReferenceDeletionEventHandler(
        ILogger<DmsArtifactUsageReferenceDeletionEventHandler> logger,
        IDmsArtifactUsageReferenceEventHandlerService dmsArtifactUsageReferenceEventHandlerService)
    {
        _logger = logger;
        _dmsArtifactUsageReferenceEventHandlerService = dmsArtifactUsageReferenceEventHandlerService;
    }
    public async Task<bool> HandleAsync(GenericEvent @event)
    {
        if (@event == null)
        {
            _logger.LogError("Event is null");
            return false;
        }
        _logger.LogInformation("Entered event handler: {HandlerName} with payload {Payload}.", nameof(DmsArtifactUsageReferenceDeletionEventHandler), @event.JsonPayload);
        try
        {
            var payload = JsonConvert.DeserializeObject<DmsArtifactUsageReferenceDeleteEventModel>(@event.JsonPayload);
            if (payload.ObjectArtifactIds?.Count == 0)
            {
                _logger.LogInformation("{HandlerName}: Operation aborted as ObjectArtifactIds are empty.", nameof(DmsArtifactUsageReferenceDeletionEventHandler));
                return false;
            }
            await _dmsArtifactUsageReferenceEventHandlerService.InitiateArtifactUsageReferenceDeletion(payload);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("Error occured while handling event: {HandlerName} with payload {Payload}. Error Message: {Message}.    Error Details: {StackTrace}", nameof(DmsArtifactUsageReferenceDeletionEventHandler), JsonConvert.SerializeObject(@event.JsonPayload), e.Message, e.StackTrace);
            return false;
        }
    }
}