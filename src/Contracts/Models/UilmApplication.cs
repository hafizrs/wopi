using MongoDB.Bson.Serialization.Attributes;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class UilmApplication
    {
        [BsonId] public string ItemId { get; set; }
        public string Name { get; set; }
        public string AppPath { get; set; }
        public int NumberOfKeys { get; set; }
        public string ModuleName { get; set; }
    }
}