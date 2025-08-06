using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.GraphQL.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.ClientEvents;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.OrganizationEvents;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class PraxisOrganizationEventHandler : IBaseEventHandlerAsync<GraphQlDataChangeEvent>
    {
        private readonly PraxisOrganizationCreatedEventHandler _createdEventHandler;
        private readonly PraxisOrganizationUpdatedEventHandler _updatedEventHandler;

        public PraxisOrganizationEventHandler(
            PraxisOrganizationCreatedEventHandler createdEventHandler,
            PraxisOrganizationUpdatedEventHandler updatedEventHandler)
        {
            _createdEventHandler = createdEventHandler;
            _updatedEventHandler = updatedEventHandler;
        }


        public async Task<bool> HandleAsync(GraphQlDataChangeEvent @event)
        {
            var eventPayload = JsonConvert.DeserializeObject<GqlEvent<PraxisOrganization>>(@event.EventTriggeredByJsonPayload);

            var eventHandler = EventHandler(@event.EventType);

            if (eventHandler != null)
            {
                return await eventHandler.HandleAsync(eventPayload);
            }

            return false;
        }

        private IBaseEventHandlerAsync<GqlEvent<PraxisOrganization>> EventHandler(string eventType)
        {
            if (eventType.Equals(PraxisEventName.OrganizationCreatedEventName))
            {
                return _createdEventHandler;
            }
            else if (eventType.Equals(PraxisEventName.OrganizationUpdatedEventName))
            {
                return _updatedEventHandler;
            }

            return null;
        }
    }
}
