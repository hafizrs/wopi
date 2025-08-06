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
    public class LibraryFileRenamedEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<LibraryFileRenamedEventHandler> _logger;
        private readonly ILibraryFileRenamedEventHandlerService _libraryFileRenamedEventHandler;

        public LibraryFileRenamedEventHandler(
            ILogger<LibraryFileRenamedEventHandler> logger,
            ILibraryFileRenamedEventHandlerService libraryFileRenamedEventHandler
        )
        {
            _logger = logger;
            _libraryFileRenamedEventHandler = libraryFileRenamedEventHandler;
        }


        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            _logger.LogInformation("Entered event handler: {EventHandlerName} -> with payload {Payload}.",
                nameof(LibraryFileRenamedEventHandler), JsonConvert.SerializeObject(@event));


            var response = false;
            try
            {
                var artifactId = JsonConvert.DeserializeObject<string>(@event.JsonPayload);

                if (!string.IsNullOrWhiteSpace(artifactId))
                {
                    response = await _libraryFileRenamedEventHandler.InitiateLibraryFileRenamedAfterEffects(artifactId);
                }
                else
                {
                    _logger.LogInformation("LibraryFileRenamedEventHandler -> Operation aborted as payload is empty.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during {EventType} event handle.", nameof(PraxisEventType.LibraryFileRenamedEvent));
                _logger.LogError("Exception Message: {ExceptionMessage}  Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled by: {EventHandlerName}.", nameof(LibraryFileRenamedEventHandler));

            return response;
        }
    }
}
