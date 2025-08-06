using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class ObjectArtifactUpdateCommand
    {
        public string ObjectArtifactId { get; set; }
        public string Color { get; set; }
        public List<string> Keywords { get; set; }
        public bool? AreAllKeywordsRemoved { get; set; }
        public string ViewMode { get; set; }
    }

}