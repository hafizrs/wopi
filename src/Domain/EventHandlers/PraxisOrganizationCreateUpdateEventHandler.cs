using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts;
using Microsoft.Extensions.Logging;
using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class PraxisOrganizationCreateUpdateEventHandler : IBaseEventHandlerAsync<GenericEvent>
    {
        private readonly ILogger<PraxisOrganizationCreateUpdateEventHandler> _logger;
        private readonly IPraxisOrganizationCreateUpdateEventService _praxisOrganizationCreateUpdateEventService;

        public PraxisOrganizationCreateUpdateEventHandler(
            ILogger<PraxisOrganizationCreateUpdateEventHandler> logger,
            IPraxisOrganizationCreateUpdateEventService praxisOrganizationCreateUpdateEventService)
        {
            _logger = logger;
            _praxisOrganizationCreateUpdateEventService = praxisOrganizationCreateUpdateEventService;
        }


        public async Task<bool> HandleAsync(GenericEvent @event)
        {
            _logger.LogInformation("Entered event handler: {EventHandlerName} -> with payload {Payload}.", nameof(PraxisOrganizationCreateUpdateEventHandler), JsonConvert.SerializeObject(@event));

            var response = false;
            if (@event != null && (@event.EventType.Equals(PraxisEventType.OrganizationCreatedEvent) || @event.EventType.Equals(PraxisEventType.OrganizationUpdatedEvent)))
            {
                try
                {
                    var organizationData = JsonConvert.DeserializeObject<PraxisOrganization>(@event.JsonPayload);

                    if (organizationData != null)
                    {
                        response = await _praxisOrganizationCreateUpdateEventService.InitiateOrganizationCreateUpdateAfterEffects(organizationData, @event.EventType);
                    }
                    else
                    {
                        _logger.LogInformation("Operation aborted as payload is empty.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Exception occurred during {EventType} event handle.", nameof(@event.EventType));
                    _logger.LogError("Exception Message: {Message}  Exception Details: {StackTrace}", ex.Message, ex.StackTrace);
                }

                _logger.LogInformation("Handled by: {EventHandlerName}.", nameof(PraxisOrganizationCreateUpdateEventHandler));
            }

            return response;
        }
    }
}
