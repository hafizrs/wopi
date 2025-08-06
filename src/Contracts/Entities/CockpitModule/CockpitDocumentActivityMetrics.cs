using MongoDB.Bson.Serialization.Attributes;
using SeliseBlocks.Genesis.Framework.PDS.Entity;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CockpitModule
{
    [BsonIgnoreExtraElements]
    public class CockpitDocumentActivityMetrics : EntityBase
    {
        public string PraxisUserId { get; set; }
        public string DepartmentId { get; set; }
        public string OrganizationId { get; set; }
        public string ActivityKey { get; set; }
        public long DocumentCount { get; set; }
        public string[] CockpitObjectArtifactSummaryIds { get; set; }
    }
}