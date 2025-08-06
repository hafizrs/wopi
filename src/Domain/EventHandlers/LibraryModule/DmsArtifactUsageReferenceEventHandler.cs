using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.LibraryModule;
using System;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.LibraryModule
{
    public class DmsArtifactUsageReferenceEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<DmsArtifactUsageReferenceEventHandler> _logger;
        private readonly IDmsArtifactUsageReferenceEventHandlerService _dmsArtifactUsageReferenceEventHandlerService;

        public DmsArtifactUsageReferenceEventHandler(
            ILogger<DmsArtifactUsageReferenceEventHandler> logger,
            IDmsArtifactUsageReferenceEventHandlerService dmsArtifactUsageReferenceEventHandlerService)
        {
            _logger = logger;
            _dmsArtifactUsageReferenceEventHandlerService = dmsArtifactUsageReferenceEventHandlerService;
        }
        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            if (@event == null) {
                _logger.LogError("Event is null");
                return false;
            }
            _logger.LogInformation("Entered event handler: {HandlerName} with payload {Payload}.", nameof(DmsArtifactUsageReferenceEventHandler), @event.JsonPayload);
            try
            {
                var payload = JsonConvert.DeserializeObject<DmsArtifactUsageReferenceEventModel>(@event.JsonPayload);
                if (payload.ObjectArtifactIds?.Count == 0)
                {
                    _logger.LogInformation("{HandlerName}: Operation aborted as ObjectArtifactIds are empty.", nameof(DmsArtifactUsageReferenceEventHandler));
                    return false;
                }
                await _dmsArtifactUsageReferenceEventHandlerService.InitiateArtifactUsageReferenceCreation(payload);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("Error occured while handling event: {HandlerName} with payload {Payload}. Error Message: {Message}.    Error Details: {StackTrace}", nameof(DmsArtifactUsageReferenceEventHandler), JsonConvert.SerializeObject(@event.JsonPayload), e.Message, e.StackTrace);
                return false;
            }
        }
    }
}