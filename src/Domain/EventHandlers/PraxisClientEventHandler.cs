using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.GraphQL.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.ClientEvents;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class PraxisClientEventHandler : IBaseEventHandlerAsync<GraphQlDataChangeEvent>
    {
        private readonly PraxisClientCreatedEventHandler createdEventHandler;
        private readonly PraxisClientUpdatedEventHandler updatedEventHandler;

        public PraxisClientEventHandler(
            PraxisClientCreatedEventHandler praxisClientCreatedEventHandler,
            PraxisClientUpdatedEventHandler praxisClientUpdatedEventHandler)
        {
            this.createdEventHandler = praxisClientCreatedEventHandler;
            this.updatedEventHandler = praxisClientUpdatedEventHandler;
        }

        public async Task<bool> HandleAsync(GraphQlDataChangeEvent @event)
        {
            var eventPayload = JsonConvert.DeserializeObject<GqlEvent<PraxisClient>>(@event.EventTriggeredByJsonPayload);

            var eventHandler = EventHandler(@event.EventType);

            return eventHandler != null && await eventHandler.HandleAsync(eventPayload);
        }

        private IBaseEventHandlerAsync<GqlEvent<PraxisClient>> EventHandler(string eventType)
        {
            if (eventType.Equals(PraxisEventName.ClientCreatedEventName))
            {
                return createdEventHandler;
            }
            else if (eventType.Equals(PraxisEventName.ClientUpdatedEventName))
            {
                return updatedEventHandler;
            }

            return null;
        }
    }
}
