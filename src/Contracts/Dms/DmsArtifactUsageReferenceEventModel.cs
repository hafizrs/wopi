using System;
using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Dms
{
    public class DmsArtifactUsageReferenceEventModel
    {
        public string Title { get; set; }
        public string RelatedEntityName { get; set; }
        public string RelatedEntityId { get; set; }
        public string PurposeEntityName { get; set; }
        public List<string> ObjectArtifactIds { get; set; }
        public DateTime? DueDate { get; set; }
        public string CompletionStatus { get; set; }
        public Dictionary<string, MetaValuePair> MetaData { get; set; } = new Dictionary<string, MetaValuePair>();
        public List<FormSpecificClientInfo> ClientInfos { get; set; }
        public string OrganizationId { get; set; }
        public List<string> OrganizationIds { get; set; }
    }
}