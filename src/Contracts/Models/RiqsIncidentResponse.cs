using MongoDB.Bson.Serialization.Attributes;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class RiqsIncidentResponse
    {
        public string Status { get; set; }
        public List<RiqsIncidentData> Data { get; set; }
    }

    public class RiqsIncidentData
    {
        [BsonId]
        public string ItemId { get; set; }
        public DateTime CreateDate { get; set; }
        public string[] Tags { get; set; }
        public string SequenceNumber { get; set; }
        public string Title { get; set; }
        public string Topic { get; set; }
        public string Status { get; set; }
        public List<StatusChangeEvent> StatusChangeLog { get; set; }
        public IEnumerable<string> KeyWords { get; set; }
        public string Description { get; set; }
        public string Measures { get; set; }
        public string Remarks { get; set; }
        public IEnumerable<string> AttachmentIds { get; set; }
        public string NextIncidentId { get; set; }
    }
}
