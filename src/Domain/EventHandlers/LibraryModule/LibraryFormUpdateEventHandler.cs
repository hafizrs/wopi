using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Microsoft.Extensions.Logging;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Dms;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.LibraryModule
{
    public class LibraryFormUpdateEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<LibraryFormUpdateEventHandler> _logger;
        private readonly ILibraryFormUpdateEventHandlerService _libraryFormUpdateEventHandlerService;

        public LibraryFormUpdateEventHandler(
            ILogger<LibraryFormUpdateEventHandler> logger,
            ILibraryFormUpdateEventHandlerService libraryFormUpdateEventHandlerService
        )
        {
            _logger = logger;
            _libraryFormUpdateEventHandlerService = libraryFormUpdateEventHandlerService;
        }


        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            _logger.LogInformation("Entered event handler: {EventHandlerName} -> with payload {Payload}.",
                nameof(LibraryFormUpdateEventHandler), JsonConvert.SerializeObject(@event));

            var response = false;
            try
            {
                var artifactId = JsonConvert.DeserializeObject<string>(@event.JsonPayload);

                if (artifactId != null)
                {
                    response = await _libraryFormUpdateEventHandlerService.InitiateLibraryFormUpdateAfterEffects(artifactId);
                }
                else
                {
                    _logger.LogInformation("LibraryFormUpdateEventHandler -> Operation aborted as payload is empty.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during {EventType} event handle.", nameof(PraxisEventType.LibraryFormUpdateEvent));
                _logger.LogError("Exception Message: {ExceptionMessage}  Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled by: {EventHandlerName}.", nameof(LibraryFormUpdateEventHandler));

            return response;
        }
    }
}
