using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class ProcessDocumentEditHtmlDocumentCommand
    {
        public string ParentObjectArtifactId { get; set; }
        public string SubscriptionId { get; set; }
        public string SubscriptionActionName { get; set; }
        public string SubscriptionContext { get; set; }
    }
}
