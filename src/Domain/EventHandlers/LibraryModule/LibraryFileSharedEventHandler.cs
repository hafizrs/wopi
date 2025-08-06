using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Microsoft.Extensions.Logging;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.LibraryModule
{
    public class LibraryFileSharedEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<LibraryFileSharedEventHandler> _logger;
        private readonly ILibraryFileSharedEventHandlerService _LibraryFileSharedEventHandlerService;

        public LibraryFileSharedEventHandler(
            ILogger<LibraryFileSharedEventHandler> logger,
            ILibraryFileSharedEventHandlerService LibraryFileSharedEventHandlerService)
        {
            _logger = logger;
            _LibraryFileSharedEventHandlerService = LibraryFileSharedEventHandlerService;
        }


        public async Task<bool> HandleAsync(GenericEvent @event)
        {
             _logger.LogInformation("Entered event handler: {EventHandlerName} -> with payload {Payload}.",
                nameof(LibraryFileSharedEventHandler), JsonConvert.SerializeObject(@event));
            var response = false;

            try
            {
                var command = JsonConvert.DeserializeObject<ObjectArtifactFileShareCommand>(@event.JsonPayload);

                if (!string.IsNullOrWhiteSpace(command.ObjectArtifactId))
                {
                    response = await _LibraryFileSharedEventHandlerService.HandleLibraryFileSharedEvent(command);
                }
                else
                {
                    _logger.LogInformation("Operation aborted as payload is empty.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during {EventType} event handle.", nameof(PraxisEventType.LibraryFileSharedEvent));
                _logger.LogError("Exception Message: {ExceptionMessage}  Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled by: {EventHandlerName}.", nameof(LibraryFileSharedEventHandler));

            return response;
        }
    }
}
