using Selise.Ecap.Entities.PrimaryEntities.Dms;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Dms
{
    public class DmsFileUploadPayload
    {
        public string FileStorageId { get; set; }
        public string Description { get; set; }
        public string[] Tags { get; set; }
        public string ParentId { get; set; }
        public string FileName { get; set; }
        public string StorageAreaId { get; set; }
        public string ObjectArtifactId { get; set; }
        public string WorkspaceId { get; set; }
        public string UserId { get; set; }
        public string OrganizationId { get; set; }
        public bool UseLicensing { get; set; }
        public int FileSizeInBytes { get; set; }
        public string FeatureId { get; set; }
        public IDictionary<string, MetaValuePair> MetaData { get; set; }
        public bool IsPreventShareWithParentSharedUsers { get; set; }
        public string CorrelationId { get; set; }

    }
}
