using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule
{
    public class CloneQuickTaskPlanCommand
    {
        public string QuickTaskId { get; set; }
        public List<string> AssignedUsers { get; set; }
        public List<DateTime> CloneToDates { get; set; }
        public bool AssignTask { get; set; }
    }
} 