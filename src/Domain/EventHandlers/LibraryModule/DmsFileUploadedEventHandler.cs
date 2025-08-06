using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Microsoft.Extensions.Logging;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class DmsFileUploadedEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<DmsFileUploadedEventHandler> _logger;
        private readonly IDmsFileUploadedEventHandlerHandlerService _dmsFileUploadedEventHandlerHandlerService;
        private readonly IDocumentKeywordService _documentKeywordService;
        private readonly INotificationService _notificationService;

        public DmsFileUploadedEventHandler(
            ILogger<DmsFileUploadedEventHandler> logger,
            IDmsFileUploadedEventHandlerHandlerService dmsFileUploadedEventHandlerHandlerService,
            IDocumentKeywordService documentKeywordService,
            INotificationService notificationService
        )
        {
            _logger = logger;
            _dmsFileUploadedEventHandlerHandlerService = dmsFileUploadedEventHandlerHandlerService;
            _documentKeywordService = documentKeywordService;
            _notificationService = notificationService;
        }


        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            _logger.LogInformation("Entered event handler: {EventHandlerName} -> with payload {Payload}.",
                nameof(DmsFileUploadedEventHandler), JsonConvert.SerializeObject(@event));

            var response = false;
            try
            {
                var fileUploadCommand = JsonConvert.DeserializeObject<ObjectArtifactFileUploadCommand>(@event.JsonPayload);
                response = await _dmsFileUploadedEventHandlerHandlerService.HandleDmsFileUploadedEvent(fileUploadCommand);
                await _documentKeywordService.UpdateObjectArtifactKeywords(fileUploadCommand.ObjectArtifactId);
                await _notificationService.GetCommonSubscriptionNotification
                (
                    response,
                    fileUploadCommand.CorrelationId,
                    "DmsFileUploadedEventHandled",
                    "DmsFileUploadedEventHandled"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred while handling event: {EventType}.", nameof(PraxisEventType.LibraryFileUploadedEvent));
                _logger.LogError("Exception Message: {ExceptionMessage} Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled by: {EventHandlerName}.", nameof(DmsFileUploadedEventHandler));


            return response;
        }
    }
}
