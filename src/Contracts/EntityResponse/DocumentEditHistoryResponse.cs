using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    public class DocumentEditHistoryResponse
    {
        public string Version { get; set; }
        public string DocSaverDisplayName { get; set; }
        public string Role { get; set; }
        public DateTime ArtifactVersionCreateDate { get; set; }
        public string VersionComparisonObjectArtifactId { get; set; }
        public string ObjectArtifactId { get; set; }
        public string VersionComparisonFileStorageId { get; set; }
        public string ParentVersion { get; set; }
        public string NewVersionComparisonObjectArtifactId { get; set; }
        public string NewVersionComparisonFileStorageId { get; set; }
    }
}
