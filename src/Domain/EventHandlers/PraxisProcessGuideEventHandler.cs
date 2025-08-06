using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using SeliseBlocks.GraphQL.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisProcessGuideEvents;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class PraxisProcessGuideEventHandler : IBaseEventHandlerAsync<GraphQlDataChangeEvent>
    {
        private readonly PraxisProcessGuideCreateEventHandler _createdEventHandler;
        private readonly PraxisProcessGuideUpdateEventHandler _updatedEventHandler;
        public PraxisProcessGuideEventHandler(
            PraxisProcessGuideCreateEventHandler createdEventHandler,
            PraxisProcessGuideUpdateEventHandler updatedEventHandler)
        {
            _createdEventHandler = createdEventHandler;
            _updatedEventHandler = updatedEventHandler;
        }

        public async Task<bool> HandleAsync(GraphQlDataChangeEvent @event)
        {
            var eventPayload = JsonConvert.DeserializeObject<GqlEvent<PraxisProcessGuide>>(@event.EventTriggeredByJsonPayload);

            var eventHandler = EventHandler(@event.EventType);

            return await eventHandler?.HandleAsync(eventPayload);
        }

        public IBaseEventHandlerAsync<GqlEvent<PraxisProcessGuide>> EventHandler(string eventType)
        {
            return eventType switch
            {
                PraxisEventName.PraxisProcessGuideCreatedEventName => _createdEventHandler,
                PraxisEventName.PraxisProcessGuideUpdatedEventName => _updatedEventHandler,
                _ => null
            };
        }
    }
}
