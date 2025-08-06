using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class ScheduledEventCommand
    {
        public bool Success { get; set; }
        public string ItemId { get; set; }
        public string Payload { get; set; }
    }

    public class ScheduledEventCommandPayload
    {
        public bool Success { get; set; }
        public string SchedulerEventType { get; set; }
    }
}
