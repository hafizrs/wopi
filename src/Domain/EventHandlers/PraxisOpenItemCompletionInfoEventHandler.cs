using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.OpenItem;
using SeliseBlocks.GraphQL.Models;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisOpenItemCompletionInfoEvents;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers
{
    public class PraxisOpenItemCompletionInfoEventHandler : IBaseEventHandlerAsync<GraphQlDataChangeEvent>
    {
        private readonly PraxisOpenItemCompletionInfoCreatedEventHandler _completionInfoCreatedEventHandler;
        private readonly PraxisOpenItemCompletionInfoUpdatedEventHandler _completionInfoUpdatedEventHandler;

        public PraxisOpenItemCompletionInfoEventHandler(
            PraxisOpenItemCompletionInfoCreatedEventHandler completionInfoCreatedEventHandler,
            PraxisOpenItemCompletionInfoUpdatedEventHandler completionInfoUpdatedEventHandler
        )
        {
            _completionInfoCreatedEventHandler = completionInfoCreatedEventHandler;
            _completionInfoUpdatedEventHandler = completionInfoUpdatedEventHandler;
        }

        public Task<bool> HandleAsync(GraphQlDataChangeEvent @event)
        {
            var eventPayload = JsonConvert.DeserializeObject<GqlEvent<PraxisOpenItemCompletionInfo>>(@event.EventTriggeredByJsonPayload);
            var eventHandler = EventHandler(@event.EventType);
            return Task.FromResult(eventHandler != null && eventHandler.Handle(eventPayload));
        }

        public IBaseEventHandler<GqlEvent<PraxisOpenItemCompletionInfo>> EventHandler(string eventType)
        {
            return eventType switch
            {
                PraxisEventName.PraxisOpenItemCompletionInfoItemCreatedEventName => _completionInfoCreatedEventHandler,
                PraxisEventName.PraxisOpenItemCompletionInfoItemUpdatedEventName => _completionInfoUpdatedEventHandler,
                _ => null
            };
        }
    }
}
