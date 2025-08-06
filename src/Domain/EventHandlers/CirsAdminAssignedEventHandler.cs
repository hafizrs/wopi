using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Microsoft.Extensions.Logging;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class CirsAdminAssignedEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<CirsAdminAssignedEventHandler> _logger;
        private readonly ICirsAdminAssignedEventHandlerService _cirsAdminAssignedEventHandlerService;

        public CirsAdminAssignedEventHandler(
            ILogger<CirsAdminAssignedEventHandler> logger,
            ICirsAdminAssignedEventHandlerService cirsAdminAssignedEventHandlerService)
        {
            _logger = logger;
            _cirsAdminAssignedEventHandlerService = cirsAdminAssignedEventHandlerService;
        }


        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            _logger.LogInformation("Entered event handler: {EventHandlerName} -> with payload {Payload}.", nameof(CirsAdminAssignedEventHandler), JsonConvert.SerializeObject(@event));

            var response = false;
            try
            {
                var dashboardPermissionId = JsonConvert.DeserializeObject<string>(@event.JsonPayload);

                if (!string.IsNullOrWhiteSpace(dashboardPermissionId))
                {
                    response = await _cirsAdminAssignedEventHandlerService
                        .InitiateAdminAssignedAfterEffectsAsync(dashboardPermissionId);
                }
                else 
                {
                    _logger.LogInformation("Operation aborted as payload is empty.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during {EventType} event handle.", nameof(PraxisEventType.CirsAdminAssignedEvent));
                _logger.LogError("Exception Message: {Message}  Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled by: {HandlerName}.", nameof(CirsAdminAssignedEventHandler));

            return response;
        }
    }
}
