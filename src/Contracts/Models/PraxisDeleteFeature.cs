using MongoDB.Bson.Serialization.Attributes;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    [BsonIgnoreExtraElements]
    public class PraxisDeleteFeature
    {
        [BsonId]
        public string ItemId { get; set; }
        public string AppType { get; set; }
        public string AppName { get; set; }
        public string FeatureId { get; set; }
        public string FeatureName { get; set; }
    }
}
