using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Task;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.GraphQL.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisTaskConfigEvents;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class PraxisTaskConfigEventHandler : IBaseEventHandlerAsync<GraphQlDataChangeEvent>
    {
        private readonly PraxisTaskConfigCreatedEventHandler createdEventHandler;
        private readonly PraxisTaskConfigUpdatedEventHandler updatedEventHandler;

        public PraxisTaskConfigEventHandler(
            PraxisTaskConfigCreatedEventHandler createdEventHandler,
            PraxisTaskConfigUpdatedEventHandler updatedEventHandler)
        {
            this.createdEventHandler = createdEventHandler;
            this.updatedEventHandler = updatedEventHandler;
        }

        public Task<bool> HandleAsync(GraphQlDataChangeEvent @event)
        {
            var eventPayload = JsonConvert.DeserializeObject<GqlEvent<PraxisTaskConfig>>(@event.EventTriggeredByJsonPayload);

            var eventHandler = EventHandler(@event.EventType);

            if (eventHandler != null)
            {
                return Task.FromResult(eventHandler.Handle(eventPayload));
            }

            return Task.FromResult(false);
        }

        public IBaseEventHandler<GqlEvent<PraxisTaskConfig>> EventHandler(string eventType)
        {
            if (eventType.Equals(PraxisEventName.PraxisTaskConfigCreatedEventName))
            {
                return createdEventHandler;
            }
            else if (eventType.Equals(PraxisEventName.PraxisTaskConfigUpdatedEventName))
            {
                return updatedEventHandler;
            }

            return null;
        }

    }
}
