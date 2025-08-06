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
    public class LibraryFolderSharedEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<LibraryFolderSharedEventHandler> _logger;
        private readonly ILibraryFolderSharedEventHandlerService _LibraryFolderSharedEventHandlerService;

        public LibraryFolderSharedEventHandler(
            ILogger<LibraryFolderSharedEventHandler> logger,
            ILibraryFolderSharedEventHandlerService LibraryFolderSharedEventHandlerService)
        {
            _logger = logger;
            _LibraryFolderSharedEventHandlerService = LibraryFolderSharedEventHandlerService;
        }


        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            _logger.LogInformation("Entered event handler: {EventHandlerName} -> with payload {Payload}.",
                nameof(LibraryFolderSharedEventHandler), JsonConvert.SerializeObject(@event));

            var response = false;
            try
            {
                var command = JsonConvert.DeserializeObject<ObjectArtifactFileShareCommand>(@event.JsonPayload);

                if (command != null && !string.IsNullOrWhiteSpace(command.ObjectArtifactId))
                {
                    response = await _LibraryFolderSharedEventHandlerService.HandleObjectArtifactFolderSharedEvent(command);
                }
                else
                {
                    _logger.LogInformation("Operation aborted as payload is empty.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during {EventType} event handle.", nameof(PraxisEventType.LibraryFolderSharedEvent));
                _logger.LogError("Exception Message: {ExceptionMessage}  Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled by: {EventHandlerName}.", nameof(LibraryFolderSharedEventHandler));

            return response;
        }
    }
}
