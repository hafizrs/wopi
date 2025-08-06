using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Microsoft.Extensions.Logging;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.LibraryModule
{
    public class LibraryRightsUpdatedEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<LibraryRightsUpdatedEventHandler> _logger;
        private readonly IRepository _repository;
        private readonly ILibraryRightsUpdatedEventHandlerService _libraryRightsUpdatedEventHandlerService;

        public LibraryRightsUpdatedEventHandler(
            ILogger<LibraryRightsUpdatedEventHandler> logger,
            IRepository repository,
            ILibraryRightsUpdatedEventHandlerService LibraryFolderSharedEventHandlerService)
        {
            _logger = logger;
            _repository = repository;
            _libraryRightsUpdatedEventHandlerService = LibraryFolderSharedEventHandlerService;
        }


        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            _logger.LogInformation("Entered event handler: {EventHandlerName} -> with payload {Payload}.",
                nameof(LibraryRightsUpdatedEventHandler), JsonConvert.SerializeObject(@event));


            var response = false;
            try
            {
                var libraryControlId = JsonConvert.DeserializeObject<string>(@event.JsonPayload);
                
                if (!string.IsNullOrWhiteSpace(libraryControlId))
                {
                    var control = await _repository.GetItemAsync<RiqsLibraryControlMechanism>(r => r.ItemId == libraryControlId);
                    if (control != null) response = await _libraryRightsUpdatedEventHandlerService.HandleLibraryRightsUpdatedEvent(control);
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

            _logger.LogInformation("Handled by: {EventHandlerName}.", nameof(LibraryRightsUpdatedEventHandler));

            return response;
        }
    }
}
