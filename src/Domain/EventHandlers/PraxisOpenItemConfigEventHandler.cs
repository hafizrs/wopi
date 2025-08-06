using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.GraphQL.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisOpenItemConfigEvents;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class PraxisOpenItemConfigEventHandler : IBaseEventHandlerAsync<GraphQlDataChangeEvent>
    {
        private readonly PraxisOpenItemConfigCreatedEventHandler createdEventHandler;
        private readonly PraxisOpenItemConfigUpdatedEventHandler updatedEventHandler;

        public PraxisOpenItemConfigEventHandler(
            PraxisOpenItemConfigUpdatedEventHandler updatedEventHandler,
            PraxisOpenItemConfigCreatedEventHandler createdEventHandler)
        {
            this.createdEventHandler = createdEventHandler;
            this.updatedEventHandler = updatedEventHandler;
        }

        public Task<bool> HandleAsync(GraphQlDataChangeEvent @event)
        {
            var eventPayload = JsonConvert.DeserializeObject<GqlEvent<PraxisOpenItemConfig>>(@event.EventTriggeredByJsonPayload);

            var eventHandler = EventHandler(@event.EventType);

            if (eventHandler != null)
            {
                return Task.FromResult(eventHandler.Handle(eventPayload));
            }

            return Task.FromResult(false);
        }

        public IBaseEventHandler<GqlEvent<PraxisOpenItemConfig>> EventHandler(string eventType)
        {
            if (eventType.Equals(PraxisEventName.PraxisOpenItemConfigCreatedEventName))
            {
                return createdEventHandler;
            }
            if (eventType.Equals(PraxisEventName.PraxisOpenItemConfigUpdatedEventName))
            {
                return updatedEventHandler;
            }
            return null;
        }

    }
}
