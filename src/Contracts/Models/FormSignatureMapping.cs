
using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class FormSignatureMapping
    {
        [BsonId] public string ItemId { get; set; }
        public string ObjectArtifactId { get; set; }
        public string DocumentId { get; set; }
        public string Url { get; set; }
        public DateTime Expired { get; set; }
        public bool IsLinkedExpired { get; set; }
    }
}
