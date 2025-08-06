using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Selise.Ecap.Entities.PrimaryEntities.Dms;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class LibraryFormMappingRecord
    {
        public string ItemId { get; set; }
        public string Name { get; set; }
        public string FileStorageId { get; set; }
        public IDictionary<string, MetaValuePair> MetaData { get; set; }
    }
}
