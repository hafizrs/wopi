using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class CloneShiftPlanCommand
    {
        public string ShiftId { get; set; }
        public List<string> PraxisUserIds { get; set; }
        public List<DateTime> CloneToDates { get; set; }
    }
}
