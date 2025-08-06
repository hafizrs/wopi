using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using System;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.LibraryModule
{
    public class DmsArtifactReapprovalEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<DmsArtifactReapprovalEventHandler> _logger;
        private readonly IDmsArtifactReapprovalEventHandlerService _dmsArtifactReapprovalEventHandlerService;
        public DmsArtifactReapprovalEventHandler(
            ILogger<DmsArtifactReapprovalEventHandler> logger,
            IDmsArtifactReapprovalEventHandlerService dmsArtifactReapprovalEventHandlerService)
        {
            _logger = logger;
            _dmsArtifactReapprovalEventHandlerService = dmsArtifactReapprovalEventHandlerService;
        }
        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            _logger.LogInformation("Entered event handler: {HandlerName} with payload {Payload}.", nameof(DmsArtifactReapprovalEventHandler), JsonConvert.SerializeObject(@event.JsonPayload));
            try
            {
                await _dmsArtifactReapprovalEventHandlerService.InitiateArtifactReapprovalEventHandler();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("Error occured while handling event: {HandlerName} with payload {Payload}. Error Message: {Message}.    Error Details: {StackTrace}", nameof(DmsArtifactReapprovalEventHandler), JsonConvert.SerializeObject(@event.JsonPayload), e.Message, e.StackTrace);
                return false;
            }
        }
    }
}
