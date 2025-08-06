using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Microsoft.Extensions.Logging;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumboard;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.CirsReport;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class CirsReportEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<CirsReportEventHandler> _logger;
        private readonly ICirsReportEventHandlerService _cirsReportEventHandlerService;

        public CirsReportEventHandler(
            ILogger<CirsReportEventHandler> logger,
            ICirsReportEventHandlerService cirsReportEventHandlerService)
        {
            _logger = logger;
            _cirsReportEventHandlerService = cirsReportEventHandlerService;
        }


        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            _logger.LogInformation("Entered event handler: {HandlerName} -> with payload {Payload}.", nameof(CirsReportEventHandler), JsonConvert.SerializeObject(@event));

            try
            {
                var cirstReportEvent= JsonConvert.DeserializeObject<CirsReportEvent>(@event.JsonPayload);

                if (!string.IsNullOrWhiteSpace(cirstReportEvent?.ReportId))
                {
                     await _cirsReportEventHandlerService
                        .ProcessEmailForCirsExternalUsers(cirstReportEvent);
                }
                else 
                {
                    _logger.LogInformation("Operation aborted as payload is empty.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occured during {nameof(PraxisEventType.CirsAdminAssignedEvent)} event handle.");
                _logger.LogError("Exception Message: {Message}  Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled by: {HandlerName}.", nameof(CirsReportEventHandler));

            return true;
        }
    }
}
