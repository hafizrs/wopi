using MongoDB.Bson.Serialization.Attributes;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using Selise.Ecap.SC.PraxisMonitor.Contracts.ResponseModels.LibraryModule;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule
{
    [BsonIgnoreExtraElements]
    public class CockpitObjectArtifactSummary : EntityBase
    {
        public string ObjectArtifactId { get; set; }
        public string Name { get; set; }
        public string OwnerId { get; set; }
        public string ParentObjectArtifactId { get; set; }
        public string FolderName { get; set; }
        public string Version { get; set; }
        public string[] Keywords { get; set; }
        public LastUpdatedSummary ObjectArtifactLastUpdatedSummary { get; set; }
        public AssigneeDetail AssigneeDetail { get; set; }
        public bool IsActive { get; set; } = true;
        public IDictionary<string, object> AdditionalInfos { get; set; }
    }

    public class LastUpdatedSummary
    {
        public DateTime UpdatedOn { get; set; }
        public string UpdatedById { get; set; }
        public string UpdatedByName { get; set; }
    }
}