using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.PraxisProcessGuideAnswerEvents;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using SeliseBlocks.GraphQL.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers;

public class PraxisProcessGuideAnswerEventHandler : IBaseEventHandlerAsync<GraphQlDataChangeEvent>
{
    private readonly PraxisProcessGuideAnswerCreatedEventHandler _praxisProcessGuideAnswerCreatedEventHandler;
    private readonly PraxisProcessGuideAnswerUpdatedEventHandler _praxisProcessGuideAnswerUpdatedEventHandler;

    public PraxisProcessGuideAnswerEventHandler(
        PraxisProcessGuideAnswerCreatedEventHandler praxisProcessGuideAnswerCreatedEventHandler,
        PraxisProcessGuideAnswerUpdatedEventHandler praxisProcessGuideAnswerUpdatedEventHandler)
    {
        _praxisProcessGuideAnswerCreatedEventHandler = praxisProcessGuideAnswerCreatedEventHandler;
        _praxisProcessGuideAnswerUpdatedEventHandler = praxisProcessGuideAnswerUpdatedEventHandler;
    }
    public async Task<bool> HandleAsync(GraphQlDataChangeEvent @event)
    {
        var eventPayload = JsonConvert.DeserializeObject<GqlEvent<PraxisProcessGuideAnswer>>(@event.EventTriggeredByJsonPayload);
        var eventHandler = EventHandler(@event.EventType);
        if (eventHandler == null) return false;
        return await eventHandler.HandleAsync(eventPayload);
    }

    private IBaseEventHandlerAsync<GqlEvent<PraxisProcessGuideAnswer>> EventHandler(string eventType)
    {
        return eventType switch
        {
            PraxisEventName.PraxisProcessGuideAnswerCreatedEventName => _praxisProcessGuideAnswerCreatedEventHandler,
            PraxisEventName.PraxisProcessGuideAnswerUpdatedEventName => _praxisProcessGuideAnswerUpdatedEventHandler,
            _ => null
        };
    }
}