using Selise.Ecap.Entities.PrimaryEntities.Dms;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Dms
{
    public class CreateFolderPayload
    {
        public string Description { get; set; }
        public string Name { get; set; }
        public string OrganizationId { get; set; }
        public bool Secured { get; set; }
        public string UserId { get; set; }
        public string WorkspaceId { get; set; }
        public string ParentId { get; set; }
        public string ParentName { get; set; }
        public string[] Tags { get; set; }
        public string ObjectArtifactId { get; set; }
        public IDictionary<string, MetaValuePair> MetaData { get; set; } = new Dictionary<string, MetaValuePair>();
        public bool IsPreventShareWithParentSharedUsers { get; set; }
        public string StorageAreaId { get; set; }
        public string CorrelationId { get; set; }
        public string Color { get; set; }
    }
}
