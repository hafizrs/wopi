using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System.Collections.Generic;
using System;
using MongoDB.Bson.Serialization.Attributes;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.CockpitModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule
{
    [BsonIgnoreExtraElements]
    public class RiqsTaskCockpitSummary : EntityBase
    {
        public List<PraxisDepartmentInfo> DepartmentDetails { get; set; }
        public string OrganizationId { get; set; }
        public string OrganizationName { get; set; }
        public string RelatedEntityId { get; set; }
        public CockpitTypeNameEnum RelatedEntityName { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Dictionary<string, object> AdditionalInfo { get; set; }
        public List<string> AssignedPraxisUserIds { get; set; } = new();
        public List<string> AssignedGroups { get; set; } = new();
        public List<PraxisUserSubmissionInfo> SubmittedBy { get; set; } = new();
        public bool IsTaskCompleted { get; set; }
        public bool IsAllUserAutoSelected { get; set; }
        public bool IsSummaryHidden { get; set; }

        public IDictionary<string, List<string>> AdditionalAssignees { get; set; } =
            new Dictionary<string, List<string>>();
        [BsonElement("DependentTasks")]
        public List<PraxisQueuedDependentTask> DependentTasks { get; set; } = new();
    }

    public class PraxisUserSubmissionInfo
    {
        public string PraxisUserId { get; set; }
        public DateTime SubmittedOn { get; set; }
    }

    public class PraxisQueuedDependentTask
    {
        public string TaskName { get; set; }
        public string TaskId { get; set; }
        public string TaskType { get; set; }
        public bool TaskStatus { get; set; }
        public DateTime DueDate { get; set; }
        public List<string> AssignedTo { get; set; }
        public List<string> ResponseSubmittedBy { get; set; }
        public List<string> RolesAllowedToRead { get; set; }
        public string ClientId { get; set; }
        public string TaskCreatedBy { get; set; }
    }
}