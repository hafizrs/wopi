using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule
{
    public class UpdateQuickTaskPlanCommand
    {
        public List<string> QuickTaskPlanIds { get; set; } = new List<string>();
        public List<string> AssignedUsers { get; set; }
    }
} 