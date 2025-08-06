using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Microsoft.Extensions.Logging;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.LibraryModule
{
    public class LibraryFileDeletedEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<LibraryFileDeletedEventHandler> _logger;
        private readonly ILibraryFileDeletedEventHandlerService _LibraryFileDeletedEventHandlerService;

        public LibraryFileDeletedEventHandler(
            ILogger<LibraryFileDeletedEventHandler> logger,
            ILibraryFileDeletedEventHandlerService LibraryFileDeletedEventHandlerService)
        {
            _logger = logger;
            _LibraryFileDeletedEventHandlerService = LibraryFileDeletedEventHandlerService;
        }

         
        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            _logger.LogInformation("Entered event handler: {EventHandlerName} -> with payload {Payload}.",
                nameof(LibraryFileDeletedEventHandler), JsonConvert.SerializeObject(@event));

            var response = false;
            try
            {
                var artifactIds = JsonConvert.DeserializeObject<List<string>>(@event.JsonPayload);

                if (artifactIds?.Count > 0)
                {
                    response = await _LibraryFileDeletedEventHandlerService.HandleLibraryFileDeletedEvent(artifactIds);
                }
                else
                {
                    _logger.LogInformation("Operation aborted as payload is empty.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during {EventType} event handle.", nameof(PraxisEventType.LibraryFileDeletedEvent));
                _logger.LogError("Exception Message: {ExceptionMessage}  Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled by: {EventHandlerName}.", nameof(LibraryFileDeletedEventHandler));

            return response;
        }
    }
}
