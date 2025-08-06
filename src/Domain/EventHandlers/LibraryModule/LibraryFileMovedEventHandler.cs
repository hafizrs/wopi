using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Microsoft.Extensions.Logging;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.LibraryModule
{
    public class LibraryFileMovedEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<LibraryFileMovedEventHandler> _logger;
        private readonly ILibraryFileMovedEventHandlerService _libraryFileMovedEventHandlerService;

        public LibraryFileMovedEventHandler(
            ILogger<LibraryFileMovedEventHandler> logger,
            ILibraryFileMovedEventHandlerService libraryFileMovedEventHandlerService)
        {
            _logger = logger;
            _libraryFileMovedEventHandlerService = libraryFileMovedEventHandlerService;
        }


        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            _logger.LogInformation("Entered event handler: {EventHandlerName} -> with payload {Payload}.",
                nameof(LibraryFileMovedEventHandler), JsonConvert.SerializeObject(@event));

            var response = false;
            try
            {
                var objectArtifactId = JsonConvert.DeserializeObject<string>(@event.JsonPayload);

                if (!string.IsNullOrWhiteSpace(objectArtifactId))
                {
                    response = await _libraryFileMovedEventHandlerService.HandleLibraryFileMovedEvent(objectArtifactId);
                }
                else
                {
                    _logger.LogInformation("Operation aborted as payload is empty.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during {EventType} event handle.", nameof(PraxisEventType.LibraryFileMovedEvent));
                _logger.LogError("Exception Message: {ExceptionMessage}  Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled by: {EventHandlerName}.", nameof(LibraryFileMovedEventHandler));

            return response;
        }
    }
}
