using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.Generic
{
    public class EntitySignatureMapping
    {
        [BsonId]
        public string ItemId { get; set; }
        public string RelatedEntityName { get; set; }
        public string RelatedEntityId { get; set; }
        public string DocumentId { get; set; }
        public string Url { get; set; }
        public DateTime Expired { get; set; }
    }
}
