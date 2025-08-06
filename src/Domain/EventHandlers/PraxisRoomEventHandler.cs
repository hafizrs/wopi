using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using SeliseBlocks.GraphQL.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisRoomEvents;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class PraxisRoomEventHandler : IBaseEventHandlerAsync<GraphQlDataChangeEvent>
    {
        private readonly PraxisRoomCreatedEventHandler createdEventHandler;
        private readonly PraxisRoomUpdatedEventHandler updatedEventHandler;

        public PraxisRoomEventHandler(
            PraxisRoomCreatedEventHandler createdEventHandler,
            PraxisRoomUpdatedEventHandler updatedEventHandler
        )
        {
            this.createdEventHandler = createdEventHandler;
            this.updatedEventHandler = updatedEventHandler;
        }
        public Task<bool> HandleAsync(GraphQlDataChangeEvent @event)
        {
            var eventPayload = JsonConvert.DeserializeObject<GqlEvent<PraxisRoom>>(@event.EventTriggeredByJsonPayload);

            var eventHandler = EventHandler(@event.EventType);

            if (eventHandler != null)
            {
                return Task.FromResult(eventHandler.Handle(eventPayload));
            }

            return Task.FromResult(false);
        }

        public IBaseEventHandler<GqlEvent<PraxisRoom>> EventHandler(string eventType)
        {
            if (eventType.Equals(PraxisEventName.PraxisRoomCreatedEventName))
            {
                return createdEventHandler;
            }
            else if (eventType.Equals(PraxisEventName.PraxisRoomUpdatedEventName))
            {
                return updatedEventHandler;
            }

            return null;
        }
    }
}
