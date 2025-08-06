using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class UilmResourceKey
    {
        [BsonId]
        public string ItemId { get; set; }
        public string AppId { get; set; }
        public string KeyName { get; set; }
        public List<Resource> Resources { get; set; }
        public string MirrorText { get; set; }
        public string Context { get; set; }
        public string Type { get; set; }
        public double Temperature { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}