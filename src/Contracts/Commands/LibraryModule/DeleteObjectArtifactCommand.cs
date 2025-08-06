using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class DeleteObjectArtifactCommand
    {
        public List<string> ObjectArtifactIds { get; set; }
    }
}
