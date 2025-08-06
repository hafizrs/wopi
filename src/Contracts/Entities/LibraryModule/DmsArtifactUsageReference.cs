using System;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.Dms;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule
{
    public class DmsArtifactUsageReference : EntityBase
    {
        public string Title { get; set; }
        public string ObjectArtifactId { get; set; }
        public string AttachmentAssignedBy { get; set; }
        public string RelatedEntityId { get; set; }
        public string RelatedEntityName { get; set; }
        public bool IsTaskCompleted { get; set; }
        public string PurposeEntityName { get; set; }
        public RelatedTaskCompletionInfo TaskCompletionInfo { get; set; }
        public Dictionary<string, MetaValuePair> MetaData { get; set; } = new Dictionary<string, MetaValuePair>();
        public List<FormSpecificClientInfo> ClientInfos { get; set; } = new List<FormSpecificClientInfo>();
        public string OrganizationId { get; set; }
        public List<string> OrganizationIds { get; set; }
    }

    public class RelatedTaskCompletionInfo
    {
        public DateTime? DueDate { get; set; }
        public string CompletionStatus { get; set; }

        public bool IsTaskCompleted =>
            string.Equals(CompletionStatus, "Completed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(CompletionStatus, "Done", StringComparison.OrdinalIgnoreCase)
            || (DueDate.HasValue && DueDate.Value < DateTime.UtcNow);
    }
}