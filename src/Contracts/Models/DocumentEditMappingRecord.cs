using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class DocumentEditMappingRecord : EntityBase
    {
        public string ParentObjectArtifactId { get; set; }
        public string ObjectArtifactId { get; set; }
        public string OriginalHtmlFileId { get; set; }
        public string CurrentHtmlFileId { get; set; }
        public string CurrentDocFileId { get; set; }
        public bool IsDocProcessing { get; set; }
        public List<DocumentEditRecordHistory> EditHistory { get; set; }
        public bool IsDraft { get; set; }
        public string Version { get; set; }
        public DateTime ArtifactVersionCreateDate { get; set; }
        public string SavedDocUserId { get; set; }
        public string SavedDocUserDisplayName { get; set; }
        public string DepartmentId { get; set; }
        public string OrganizationId { get; set; }
        public bool IsUploadedFromWeb { get; set; }
        public string FileType { get; set; }
        public string VersionComparisonObjectArtifactId { get; set; }
        public string VersionComparisonFileStorageId { get; set; }
        public string ParentVersion { get; set; }
        public string NewVersionComparisonObjectArtifactId { get; set; }
        public string NewVersionComparisonFileStorageId { get; set; }
    }

    public class DocumentEditRecordHistory
    {
        public string EditorUserId { get; set; }
        public string EditorDisplayName { get; set; }
        public DateTime EditDate { get; set; }
    }

    public class ArtifactParentChildVersionLevel
    {
        public bool IsSameLevelVersion { get; set; }
        public bool IsParentFloating { get; set; }
        public bool IsChildFloating { get; set; }
        public double ChildVersion { get; set; }
        public double ParentVersion { get; set; }
    }
}
