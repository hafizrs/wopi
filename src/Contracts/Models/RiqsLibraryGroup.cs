using MongoDB.Bson.Serialization.Attributes;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    [BsonIgnoreExtraElements]
    public class RiqsLibraryGroup : EntityBase
    {
        public string OrganizationId { get; set; }
        public string Name { get; set; }
        public string ParentId { get; set; }
        public LibraryGroupType GroupType { get; set; }
    }
}
