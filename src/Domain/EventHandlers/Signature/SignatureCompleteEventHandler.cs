using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.ESignature.Service.Events;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using System;
using System.Threading.Tasks;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.ConfiguratorModule;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Signature
{
    public class SignatureCompleteEventHandler : IEventHandler<ExternalContractSentAndSignedEvent, bool>
    {
        private readonly ILibraryFormService _libraryFormService;
        private readonly ILogger<SignatureCompleteEventHandler> _logger;
        private readonly IReportTemplateSignatureService _reportTemplateSignatureService;

        public SignatureCompleteEventHandler(
            ILibraryFormService libraryFormService, ILogger<SignatureCompleteEventHandler> logger,
            IReportTemplateSignatureService reportTemplateSignatureService
        )
        {
            _libraryFormService = libraryFormService;
            _logger = logger;
            _reportTemplateSignatureService = reportTemplateSignatureService; 
        }

        [Invocable]
        public bool Handle(ExternalContractSentAndSignedEvent @event)
        {
            return HandleAsync(@event).Result;
        }

        [Invocable]
        public async Task<bool> HandleAsync(ExternalContractSentAndSignedEvent @event)
        {
            var response = false;
            try
            {
                _logger.LogInformation("Enter {ClassName} with payload: {Payload}",
                    nameof(SignatureCompleteEventHandler), JsonConvert.SerializeObject(@event));

                if (@event == null || string.IsNullOrEmpty(@event?.DocumentId) || @event.FileMaps?.Count == 0)
                {
                    return false;
                }

                var reportItemId = await _reportTemplateSignatureService.GetRelatedEntityIdFromSignatureMappingByDocumentId(@event.DocumentId);

                if (await _reportTemplateSignatureService.IsReportExistsAsync(reportItemId))
                {
                    return await _reportTemplateSignatureService.CompleteSignatureProcessAsync(@event);
                }

                response = await _libraryFormService.CompleteFormSignature(@event);

            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred in {ClassName} -> {MethodName}",
                    nameof(SignatureCompleteEventHandler), nameof(HandleAsync));

                _logger.LogError("Exception Message: {ExMessage} Exception Details: {ExStackTrace}", ex.Message,
                    ex.StackTrace);
            }

            return response;
        }

    }
}
