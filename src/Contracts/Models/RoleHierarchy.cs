using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class RoleHierarchy
    {
        [BsonId]
        public string ItemId { get; set; }
        public string Role { get; set; }
        public List<string> Parents { get; set; }
    }
}
