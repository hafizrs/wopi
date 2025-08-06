using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.Events
{
    public class UpdateProcessGuideCompletionStatusEvent
    {
        public List<string> ProcessGuideIds { get; set; }
        public string NotificationSubscriptionId { get; set; }
    }
}