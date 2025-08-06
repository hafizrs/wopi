using System;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.LibraryModule
{
    public class LibraryFileEditedByOthersEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<LibraryFileEditedByOthersEventHandler> _logger;
        private readonly ILibraryFileEditedByOthersEventHandlerService _libraryFileEditedByOthersEventHandlerService;

        public LibraryFileEditedByOthersEventHandler(
            ILogger<LibraryFileEditedByOthersEventHandler> logger,
            ILibraryFileEditedByOthersEventHandlerService libraryFileEditedByOthersEventHandlerService)
        {
            _logger = logger;
            _libraryFileEditedByOthersEventHandlerService = libraryFileEditedByOthersEventHandlerService;
        }
        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            _logger.LogInformation("Entered event handler: {HandlerName} -> with payload {Payload}.", nameof(LibraryFileEditedByOthersEventHandler), JsonConvert.SerializeObject(@event.JsonPayload));
            var response = false;
            try
            {
                var objectArtifactId = JsonConvert.DeserializeObject<string>(@event.JsonPayload);
                if (!string.IsNullOrWhiteSpace(objectArtifactId))
                {
                    _logger.LogInformation("Calling the service : {ServiceName} to handle {HandlerName} with ObjectArtifactId: {ObjectArtifactId}.", nameof(LibraryFileEditedByOthersEventHandler), nameof(_libraryFileEditedByOthersEventHandlerService), objectArtifactId);
                    
                    response = await _libraryFileEditedByOthersEventHandlerService.HandleLibraryFileEditedByOthersEvent(objectArtifactId);

                    _logger.LogInformation("Returned from Service: {ServiceName} after handling {HandlerName} with ObjectArtifactId: {ObjectArtifactId}.", nameof(_libraryFileEditedByOthersEventHandlerService), nameof(LibraryFileEditedByOthersEventHandler), objectArtifactId);
                }
                else
                {
                    _logger.LogInformation("Operation aborted in Handler: {HandlerName} as ObjectArtifactId is null or empty: {objectArtifactId}.", nameof(LibraryFileEditedByOthersEventHandler), objectArtifactId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Exception in Handler: {HandlerName} Error Message: {Message} Error Details: {StackTrace}", nameof(LibraryFileEditedByOthersEventHandler), e.Message, e.StackTrace);
            }
            _logger.LogInformation("Handled by: {HandlerName}.", nameof(LibraryFileEditedByOthersEventHandler));
            return response;
        }
    }
}