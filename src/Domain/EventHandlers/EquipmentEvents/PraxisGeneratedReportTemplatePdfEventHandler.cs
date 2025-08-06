using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.ConfiguratorModule;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.Genesis.Framework.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.EquipmentEvents
{
    public class PraxisGeneratedReportTemplatePdfEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<PraxisGeneratedReportTemplatePdfEventHandler> _logger;
        private readonly IPraxisReportTemplateService _praxisReportTemplateService;

        public PraxisGeneratedReportTemplatePdfEventHandler(ILogger<PraxisGeneratedReportTemplatePdfEventHandler> logger, IPraxisReportTemplateService praxisReportTemplateService)
        {
            _logger = logger;
            _praxisReportTemplateService = praxisReportTemplateService;
        }

        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            _logger.LogInformation("Entered {HandlerName} with payload: {Payload}", nameof(PraxisGeneratedReportTemplatePdfEventHandler), JsonConvert.SerializeObject(@event, Formatting.Indented));
            try
            {
                if (string.IsNullOrWhiteSpace(@event.JsonPayload))
                {
                    _logger.LogWarning("JsonPayload is null or empty in {HandlerName}.", nameof(PraxisGeneratedReportTemplatePdfEventHandler));
                    return false;
                }
                var payload = JsonConvert.DeserializeObject<string>(@event.JsonPayload);
                await _praxisReportTemplateService.PrepareHtmlCommandAndGeneratePdf(payload);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("Error occurred in {HandlerName}. Error Message: {Message}. Error Details: {StackTrace}", 
                    nameof(PraxisGeneratedReportTemplatePdfEventHandler), e.Message, e.StackTrace);
                return false;
            }
        }
    }
}
