using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.GraphQL.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisFormEvents;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class PraxisFormEventHandler : IBaseEventHandlerAsync<GraphQlDataChangeEvent>
    {
        private readonly PraxisFormCreatedEventHandler createdEventHandler;
        private readonly PraxisFormUpdatedEventHandler updatedEventHandler;
        public PraxisFormEventHandler(
            PraxisFormCreatedEventHandler createdEventHandler,
            PraxisFormUpdatedEventHandler updatedEventHandler)
        {
            this.createdEventHandler = createdEventHandler;
            this.updatedEventHandler = updatedEventHandler;
        }

        public Task<bool> HandleAsync(GraphQlDataChangeEvent @event)
        {
            var eventPayload = JsonConvert.DeserializeObject<GqlEvent<PraxisForm>>(@event.EventTriggeredByJsonPayload);

            var eventHandler = EventHandler(@event.EventType);

            if (eventHandler != null)
            {
                return Task.FromResult(eventHandler.Handle(eventPayload));
            }

            return Task.FromResult(false);
        }

        public IBaseEventHandler<GqlEvent<PraxisForm>> EventHandler(string eventType)
        {
            if (eventType.Equals(PraxisEventName.PraxisFormCreatedEventName))
            {
                return createdEventHandler;
            }
            else if (eventType.Equals(PraxisEventName.PraxisFormUpdatedEventName))
            {
                return updatedEventHandler;
            }

            return null;
        }
    }
}
