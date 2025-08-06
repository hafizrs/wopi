using SeliseBlocks.Genesis.Framework.Events;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.Events
{
    public class FileConversionCompletedEvent : GenericEvent
    {
        public Request Request { get; set; }
    }
}
