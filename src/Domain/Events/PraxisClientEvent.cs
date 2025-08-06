using SeliseBlocks.Genesis.Framework.Events;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.Events
{
    public class PraxisClientEvent : GenericEvent
    {
       public string EventTriggeredByJsonPayload { get; set; }
    }
}
