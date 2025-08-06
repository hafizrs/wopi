using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Dms
{
    public class DmsArtifactUsageReferenceDtoModel
    {
        public DmsArtifactUsageReferenceDtoModel(DmsArtifactUsageReference artifact = null)
        {
            if (artifact == null) return;
            Title = artifact.Title;
            AttachmentAssignedBy = artifact.AttachmentAssignedBy;
            RelatedEntityId = artifact.RelatedEntityId;
            PurposeEntityName = artifact.PurposeEntityName;
            MetaData = artifact.MetaData;
            OrganizationId = artifact.OrganizationId;
            ClientInfos = artifact.ClientInfos;
        }
        public string Title { get; set; }
        public string AttachmentAssignedBy { get; set; }
        public string RelatedEntityId { get; set; }
        public string PurposeEntityName { get; set; }
        public Dictionary<string, MetaValuePair> MetaData { get; set; }
        public string OrganizationId { get; set; }
        public List<FormSpecificClientInfo> ClientInfos { get; set; }
    }
}