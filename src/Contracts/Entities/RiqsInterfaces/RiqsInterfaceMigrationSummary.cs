using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.RiqsInterfaces
{
    public class RiqsInterfaceMigrationSummary : EntityBase
    {
        public string Provider { get; set; }
        public string ClientId { get; set; }
        public string OrganizationId { get; set; }
        public List<InterfaceFolder> InterfaceFolders { get; set; }
        public List<InterfaceFile> InterfaceFiles { get; set; }
    }

    public class InterfaceFolder
    {
        public string FolderId { get; set; }
        public string Name { get; set; }
        public string ParentId { get; set; }
        public string SiteId { get; set; }
        public string Path { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime CreateDate { get; set; }
        public string ArtifactId { get; set; }
    }

    public class InterfaceFile
    {
        public string FileId { get; set; }
        public string Name { get; set; }
        public string ParentId { get; set; }
        public string Path { get; set; }
        public string SiteId { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime CreateDate { get; set; }
        public string ArtifactId { get; set; }
        public long FileSize { get; set; }
        public string DownloadUrl { get; set; }
    }
}
