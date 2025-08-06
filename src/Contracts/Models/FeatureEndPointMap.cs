using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class FeatureEndPointMap
    {
        [BsonId] 
        public ObjectId ItemId { get; set; }
        public string AppType { get; set; }
        public string AppName { get; set; }
        public string FeatureId { get; set; }
        public string FeatureName { get; set; }
        public string ResourceId { get; set; }
        public string Service { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string ResourcePath { get; set; }


        public FeatureEndPointMap()
        {
        }

        public FeatureEndPointMap(IDictionary<string, string> datum)
        {
            ItemId = datum.ContainsKey("ItemId") ? new ObjectId(datum["ItemId"]) : ObjectId.GenerateNewId();
            AppType = datum.ContainsKey("AppType") ? datum["AppType"] : string.Empty;
            AppName = datum.ContainsKey("AppName") ? datum["AppName"] : string.Empty;
            FeatureId = datum.ContainsKey("FeatureId") ? datum["FeatureId"] : string.Empty;
            FeatureName = datum.ContainsKey("FeatureName") ? datum["FeatureName"] : string.Empty;
            ResourceId = datum.ContainsKey("ResourceId") ? datum["ResourceId"] : string.Empty;
            Service = datum.ContainsKey("Service") ? datum["Service"] : string.Empty;
            Controller = datum.ContainsKey("Controller") ? datum["Controller"] : string.Empty;
            Action = datum.ContainsKey("Action") ? datum["Action"] : string.Empty;
            ResourcePath = datum.ContainsKey("ResourcePath") ? datum["ResourcePath"] : string.Empty;
        }
    }
}