using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.GraphQL.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisOpenItemEvents;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class PraxisOpenItemEventHandler : IBaseEventHandlerAsync<GraphQlDataChangeEvent>
    {
        private readonly PraxisOpenItemCreatedEventHandler createdEventHandler;
        private readonly PraxisOpenItemUpdatedEventHandler updatedEventHandler;
        public PraxisOpenItemEventHandler(
            PraxisOpenItemCreatedEventHandler createdEventHandler,
            PraxisOpenItemUpdatedEventHandler updatedEventHandler
            )
        {
            this.createdEventHandler = createdEventHandler;
            this.updatedEventHandler = updatedEventHandler;
        }

        public async Task<bool> HandleAsync(GraphQlDataChangeEvent @event)
        {
            var eventPayload = JsonConvert.DeserializeObject<GqlEvent<PraxisOpenItem>>(@event.EventTriggeredByJsonPayload);
            var eventHandler = EventHandler(@event.EventType);
            if (eventHandler != null)
            {
                await eventHandler.HandleAsync(eventPayload);
            }
            return true;
        }

        private IBaseEventHandlerAsync<GqlEvent<PraxisOpenItem>> EventHandler(string eventType)
        {
            return eventType switch
            {
                PraxisEventName.PraxisOpenItemCreatedEventName => createdEventHandler,
                PraxisEventName.PraxisOpenItemUpdatedEventName => updatedEventHandler,
                _ => null
            };
        }
    }
}
