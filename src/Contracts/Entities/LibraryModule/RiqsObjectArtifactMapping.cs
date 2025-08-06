using MongoDB.Bson.Serialization.Attributes;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule
{
    [BsonIgnoreExtraElements]
    public class RiqsObjectArtifactMapping: EntityBase
    {
        public string ObjectArtifactId { get; set; }
        public string OrganizationId { get; set; }
        public List<ObjectArifactApproverInfo> ApproverInfos { get; set; }
        public List<UserPraxisUserIdPair> ApprovalAdmins { get; set; }
        public List<UserPraxisUserIdPair> UploadAdmins { get; set; }
        public List<RiqsActivitySummaryModel> FormCompletionSummary { get; set; }
    }

    public class ObjectArifactApproverInfo
    {
        public string ApproverId { get; set; }
        public string ApproverName { get; set; }
        public DateTime ApprovedDate { get; set; }
        public int ReapprovalCount { get; set; }
    }

    public class RiqsActivitySummaryModel
    {
        public string OrganizationId { get; set; }
        public string FilledFormId { get; set; }
        public string PerformedBy { get; set; }
        public DateTime PerformedOn { get; set; }
    }
}
