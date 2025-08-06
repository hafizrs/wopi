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
    public class LibraryFileApprovedEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<LibraryFileApprovedEventHandler> _logger;
        private readonly ILibraryFileApprovedEventHandlerService _libraryFileApprovedEventHandler;

        public LibraryFileApprovedEventHandler(
            ILogger<LibraryFileApprovedEventHandler> logger,
            ILibraryFileApprovedEventHandlerService libraryFileApprovedEventHandler
        )
        {
            _logger = logger;
            _libraryFileApprovedEventHandler = libraryFileApprovedEventHandler;
        }


        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            _logger.LogInformation("Entered event handler: {EventHandlerName} -> with payload {Payload}.",
             nameof(LibraryFileApprovedEventHandler), JsonConvert.SerializeObject(@event));

            var response = false;
            try
            {
                var artifactId = JsonConvert.DeserializeObject<string>(@event.JsonPayload);

                if (!string.IsNullOrWhiteSpace(artifactId))
                {
                    response = await _libraryFileApprovedEventHandler.InitiateLibraryFileApprovedAfterEffects(artifactId);
                }
                else
                {
                    _logger.LogInformation("LibraryFileApprovedEventHandler -> Operation aborted as payload is empty.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during {EventType} event handle.", nameof(PraxisEventType.LibraryFileApprovedEvent));
                _logger.LogError("Exception Message: {ExceptionMessage}  Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled by: {EventHandlerName}.", nameof(LibraryFileApprovedEventHandler));

            return response;
        }
    }
}
