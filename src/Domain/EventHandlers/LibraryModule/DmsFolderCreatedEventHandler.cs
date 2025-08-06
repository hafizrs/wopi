using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Microsoft.Extensions.Logging;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class DmsFolderCreatedEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<DmsFolderCreatedEventHandler> _logger;
        private readonly IDmsFolderCreatedEventHandlerHandlerService _dmsFolderCreatedEventHandlerHandlerService;
        private readonly INotificationService _notificationService;

        public DmsFolderCreatedEventHandler(
            ILogger<DmsFolderCreatedEventHandler> logger,
            IDmsFolderCreatedEventHandlerHandlerService dmsFolderCreatedEventHandlerHandlerService,
            INotificationService notificationService)
        {
            _logger = logger;
            _dmsFolderCreatedEventHandlerHandlerService = dmsFolderCreatedEventHandlerHandlerService;
            _notificationService = notificationService;
        }

        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            _logger.LogInformation(
                "Entered event handler: {EventHandlerName} -> with payload {Payload}.",
                nameof(DmsFolderCreatedEventHandler),
                JsonConvert.SerializeObject(@event)
            );

            var response = false;
            try
            {
                var fileUploadCommand = JsonConvert.DeserializeObject<ObjectArtifactFolderCreateCommand>(@event.JsonPayload);
                response = await _dmsFolderCreatedEventHandlerHandlerService.HandleDmsFolderCreatedEvent(fileUploadCommand);
                await _notificationService.GetCommonSubscriptionNotification
                (
                    response, 
                    fileUploadCommand.CorrelationId, 
                    "DmsFolderCreatedEventHandled", 
                    "DmsFolderCreatedEventHandled"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred while handling event: {EventName}.", nameof(PraxisEventType.LibraryFolderCreatedEvent));
                _logger.LogError("Exception Message: {ExceptionMessage} Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled by: {EventHandlerName}.", nameof(DmsFolderCreatedEventHandler));


            return response;
        }
    }
}
