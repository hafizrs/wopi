using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class AddRemoveCommand
    {
        public List<string> AddedIds { get; set; } = new List<string>();
        public List<string> RemovedIds { get; set; } = new List<string>();
    }
}
