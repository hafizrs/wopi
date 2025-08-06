using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Selise.Ecap.Entities.PrimaryEntities.Dms;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class LibraryFormCloneCommand
    {
        [Required] public string ParentObjectArtifactId { get; set; }
        [Required] public string NewObjectArtifactId { get; set; }
        [Required] public string FileStorageId { get; set; }
        public string SubscriptionId { get; set; }
        [Required] public string WorkspaceId { get; set; }
        [Required] public string OrganizationId { get; set; }
        [Required] public bool UseLicensing { get; set; }
        public IDictionary<string, MetaValuePair> MetaData { get; set; }
    }
}
