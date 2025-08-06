using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.GraphQL.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisUserEvents;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class PraxisUserEventHandler : IBaseEventHandlerAsync<GraphQlDataChangeEvent>
    {
        private readonly PraxisUserCreatedEventHandler createdEventHandler;
        private readonly PraxisUserUpdatedEventHandler updatedEventHandler;

        public PraxisUserEventHandler(
            PraxisUserCreatedEventHandler createdEventHandler,
            PraxisUserUpdatedEventHandler updatedEventHandler
        )
        {
            this.createdEventHandler = createdEventHandler;
            this.updatedEventHandler = updatedEventHandler;
        }

        public Task<bool> HandleAsync(GraphQlDataChangeEvent @event)
        {
            var eventPayload = JsonConvert.DeserializeObject<GqlEvent<PraxisUser>>(@event.EventTriggeredByJsonPayload);

            var eventHandler = EventHandler(@event.EventType);

            if (eventHandler != null)
            {
                return Task.FromResult(eventHandler.Handle(eventPayload));
            }

            return Task.FromResult(false);
        }

        private IBaseEventHandler<GqlEvent<PraxisUser>> EventHandler(string eventType)
        {
            if (eventType.Equals(PraxisEventName.PraxisUserCreatedEventName))
            {
                return createdEventHandler;
            }
            else if (eventType.Equals(PraxisEventName.PraxisUserUpdatedEventName))
            {
                return updatedEventHandler;
            }

            return null;
        }
    }
}
