using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.GraphQL.Models;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisTaskEvents;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class PraxisTaskEventHandler : IBaseEventHandlerAsync<GraphQlDataChangeEvent>
    {
        private readonly PraxisTaskCreatedEventHandler createdEventHandler;
        private readonly PraxisTaskUpdatedEventHandler updatedEventHandler;
        public PraxisTaskEventHandler(
            PraxisTaskCreatedEventHandler createdEventHandler,
            PraxisTaskUpdatedEventHandler updatedEventHandler)
        {
            this.createdEventHandler = createdEventHandler;
            this.updatedEventHandler = updatedEventHandler;
        }

        public Task<bool> HandleAsync(GraphQlDataChangeEvent @event)
        {
            var eventPayload = JsonConvert.DeserializeObject<GqlEvent<PraxisTask>>(@event.EventTriggeredByJsonPayload);

            var eventHandler = EventHandler(@event.EventType);

            if (eventHandler != null)
            {
                return Task.FromResult(eventHandler.Handle(eventPayload));
            }

            return Task.FromResult(false);
        }

        public IBaseEventHandler<GqlEvent<PraxisTask>> EventHandler(string eventType)
        {
            if (eventType.Equals(PraxisEventName.PraxisTaskCreatedEventName))
            {
                return createdEventHandler;
            }
            else if (eventType.Equals(PraxisEventName.PraxisTaskUpdatedEventName))
            {
                return updatedEventHandler;
            }

            return null;
        }
    }
}
