using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.ESignature.Service.Events;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.ConfiguratorModule;
using MongoDB.Driver;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Signature
{
    public class SignatureProcessedEventHandler : IEventHandler<ExternalContractProcessedEvent, bool>
    {
        private readonly ILibraryFormService _libraryFormService;
        private readonly ILogger<SignatureProcessedEventHandler> _logger;
        private readonly IReportTemplateSignatureService _reportTemplateSignatureService;

        public SignatureProcessedEventHandler(ILibraryFormService libraryFormService,
            ILogger<SignatureProcessedEventHandler> logger,
            IReportTemplateSignatureService reportTemplateSignatureService)
        {
            _libraryFormService = libraryFormService;
            _logger = logger;
            _reportTemplateSignatureService = reportTemplateSignatureService;
        }

        [Invocable]
        public bool Handle(ExternalContractProcessedEvent @event)
        {
            return HandleAsync(@event).Result;
        }

        [Invocable]
        public async Task<bool> HandleAsync(ExternalContractProcessedEvent @event)
        {
            var response = false;
            try
            {
                _logger.LogInformation("Enter {ClassName} with payload: {Payload}",
                    nameof(SignatureProcessedEventHandler), JsonConvert.SerializeObject(@event));

                if (@event == null || string.IsNullOrEmpty(@event?.Response?.AccessLink) ||
                    string.IsNullOrEmpty(@event.Response.TrackingId))
                {
                    return false;
                }

                var isReportTemplate = await _reportTemplateSignatureService.IsReportExistsAsync(@event.Response.TrackingId);

                _logger.LogInformation("Is Report Template: {IsReportTemplate}", isReportTemplate);

                if (isReportTemplate)
                {
                    await _reportTemplateSignatureService.UpdateSignatureUrlAsync(@event.Response);
                    return true;
                }

                response = await _libraryFormService.UpdateFormSignatureUrl(@event);

            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred in {ClassName} -> {MethodName}",
                    nameof(SignatureProcessedEventHandler), nameof(HandleAsync));

                _logger.LogError("Exception Message: {ExMessage} Exception Details: {ExStackTrace}", ex.Message,
                    ex.StackTrace);
            }

            return response;
        }
    }
}
