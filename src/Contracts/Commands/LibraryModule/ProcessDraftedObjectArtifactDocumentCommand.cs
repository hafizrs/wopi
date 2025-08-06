using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class ProcessDraftedObjectArtifactDocumentCommand
    {
        public string HtmlFileId { get; set; }
        public string ObjectArtifactId { get; set; }
        public string WorkspaceId { get; set; }
        public string OrganizationId { get; set; }
        public bool UseLicensing { get; set; }
        public bool? IsPreventShareWithParentSharedUsers { get; set; }
        public string SubscriptionId { get; set; }
        public string SubscriptionActionName { get; set; }
        public string SubscriptionContext { get; set; }
        public bool IsNotifyToCockpit { get; set; }
    }
}
