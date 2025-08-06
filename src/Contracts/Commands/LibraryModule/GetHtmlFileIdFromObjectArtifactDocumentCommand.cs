using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class GetHtmlFileIdFromObjectArtifactDocumentCommand
    {
        public string ObjectArtifactId { get; set; }
        public string SubscriptionId { get; set; }
    }
}
