using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule
{
    public class UpdateQuickTaskSequenceCommand
    {
        public List<string> QuickTaskIds { get; set; }
    }
} 