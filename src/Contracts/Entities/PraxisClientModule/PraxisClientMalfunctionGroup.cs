using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.PraxisClientModule
{
    [BsonIgnoreExtraElements]
    public class PraxisClientMalfunctionGroup : EntityBase
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string OrganizationId { get; set; }
        public string ClientId { get; set; }
        public List<string> ControllingGroup { get; set; }
        public List<string> ControlledGroup { get; set; }
        public List<string> MalfunctionTypes { get; set; }
        public List<string> MalfunctionSubTypes { get; set; }
        public IDictionary<string, object> AdditionalInfo { get; set; }
        public bool IsActive { get; set; } = true;

    }
}
