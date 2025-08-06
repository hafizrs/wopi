using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class ObjectArtifactMoveCommand
    {
        public List<string> ObjectArtifactIds { get; set; }
        public string NewParentId { get; set; }
    }

}