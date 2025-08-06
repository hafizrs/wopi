using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class FeatureRoleMap
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ItemId { get; set; }
        public string AppType { get; set; }
        public string AppName { get; set; }
        public string FeatureId { get; set; }
        public string FeatureName { get; set; }
        public string RoleName { get; set; }


        public FeatureRoleMap()
        {
        }

        public FeatureRoleMap(IDictionary<string, string> datum)
        {
            ItemId = datum.ContainsKey("ItemId") ? datum["ItemId"] : ObjectId.GenerateNewId().ToString();
            AppType = datum.ContainsKey("AppType") ? datum["AppType"] : string.Empty;
            AppName = datum.ContainsKey("AppName") ? datum["AppName"] : string.Empty;
            FeatureId = datum.ContainsKey("FeatureId") ? datum["FeatureId"] : string.Empty;
            FeatureName = datum.ContainsKey("FeatureName") ? datum["FeatureName"] : string.Empty;
        }
    }
}