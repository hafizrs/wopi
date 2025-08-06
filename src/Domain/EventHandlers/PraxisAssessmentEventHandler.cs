using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.AssessmentEvents;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using SeliseBlocks.GraphQL.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class PraxisAssessmentEventHandler : IBaseEventHandlerAsync<GraphQlDataChangeEvent>
    {
        private readonly PraxisAssessmentCreatedEventHandler createdEventHandler;
        private readonly PraxisAssessmentUpdatedEventHandler updatedEventHandler;
        public PraxisAssessmentEventHandler(
            PraxisAssessmentCreatedEventHandler createdEventHandler,
            PraxisAssessmentUpdatedEventHandler updatedEventHandler)
        {
            this.createdEventHandler = createdEventHandler;
            this.updatedEventHandler = updatedEventHandler;
        }

        public Task<bool> HandleAsync(GraphQlDataChangeEvent @event)
        {
            var eventPayload = JsonConvert.DeserializeObject<GqlEvent<PraxisAssessment>>(@event.EventTriggeredByJsonPayload);

            var eventHandler = EventHandler(@event.EventType);

            if (eventHandler != null)
            {
                return Task.FromResult(eventHandler.Handle(eventPayload));
            }

            return Task.FromResult(false);
        }

        public IBaseEventHandler<GqlEvent<PraxisAssessment>> EventHandler(string eventType)
        {
            if (eventType.Equals(PraxisEventName.PraxisAssessmentCreatedEventName))
            {
                return createdEventHandler;
            }
            else if (eventType.Equals(PraxisEventName.PraxisAssessmentUpdatedEventName))
            {
                return updatedEventHandler;
            }

            return null;
        }
    }
}
