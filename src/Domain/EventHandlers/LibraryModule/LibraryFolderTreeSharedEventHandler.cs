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
    public class LibraryFolderTreeSharedEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<LibraryFolderTreeSharedEventHandler> _logger;
        private readonly ILibraryFolderTreeSharedEventHandlerService _libraryFolderTreeSharedEventHandlerService;

        public LibraryFolderTreeSharedEventHandler(
            ILogger<LibraryFolderTreeSharedEventHandler> logger,
            ILibraryFolderTreeSharedEventHandlerService libraryFolderTreeSharedEventHandlerService)
        {
            _logger = logger;
            _libraryFolderTreeSharedEventHandlerService = libraryFolderTreeSharedEventHandlerService;
        }


        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            _logger.LogInformation("Entered event handler: {EventHandlerName} -> with payload {Payload}.",
               nameof(LibraryFolderTreeSharedEventHandler), JsonConvert.SerializeObject(@event));

            var response = false;
            try
            {
                var objectArtifactIds = JsonConvert.DeserializeObject<string[]>(@event.JsonPayload);

                if (objectArtifactIds?.Length > 0)
                {
                    response = await _libraryFolderTreeSharedEventHandlerService.HandleLibraryFolderTreeSharedEvent(objectArtifactIds);
                }
                else
                {
                    _logger.LogInformation("Operation aborted as payload is empty.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during {EventType} event handle.", nameof(PraxisEventType.LibraryFolderTreeSharedEvent));
                _logger.LogError("Exception Message: {ExceptionMessage}  Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled by: {EventHandlerName}.", nameof(LibraryFolderTreeSharedEventHandler));

            return response;
        }
    }
}