using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule
{
    public class DeleteQuickTaskPlanCommand
    {
        public List<string> QuickTaskPlanIds { get; set; }
    }
} 