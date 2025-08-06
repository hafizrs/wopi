using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.GraphQL.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.RiskManagementEvents;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class PraxisRiskEventHandler : IBaseEventHandlerAsync<GraphQlDataChangeEvent>
    {
        private readonly PraxisRiskCreatedEventHandler createdEventHandler;
        private readonly PraxisRiskUpdatedEventHandler updatedEventHandler;
        public PraxisRiskEventHandler(
            PraxisRiskCreatedEventHandler createdEventHandler,
            PraxisRiskUpdatedEventHandler updatedEventHandler)
        {
            this.createdEventHandler = createdEventHandler;
            this.updatedEventHandler = updatedEventHandler;
        }

        public Task<bool> HandleAsync(GraphQlDataChangeEvent @event)
        {
            var eventPayload = JsonConvert.DeserializeObject<GqlEvent<PraxisRisk>>(@event.EventTriggeredByJsonPayload);

            var eventHandler = EventHandler(@event.EventType);

            if (eventHandler != null)
            {
                return Task.FromResult(eventHandler.Handle(eventPayload));
            }

            return Task.FromResult(false);
        }

        public IBaseEventHandler<GqlEvent<PraxisRisk>> EventHandler(string eventType)
        {
            if (eventType.Equals(PraxisEventName.PraxisRiskCreatedEventName))
            {
                return createdEventHandler;
            }
            else if (eventType.Equals(PraxisEventName.PraxisRiskUpdatedEventName))
            {
                return updatedEventHandler;
            }

            return null;
        }
    }
}
