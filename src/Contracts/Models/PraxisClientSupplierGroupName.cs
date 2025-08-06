using MongoDB.Bson.Serialization.Attributes;
using SeliseBlocks.Genesis.Framework.PDS.Entity;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    [BsonIgnoreExtraElements]
    public class PraxisClientSupplierGroupName : EntityBase
    {
        public string PraxisClientId { get; set; }
        public SupplierGroupNameModel SupplierGroupName { get; set; }
    }


    public class SupplierGroupNameModel
    {
        public string MedicalSupplierLabel { get; set; }
        public string SuppliersInfrastructrueLabel { get; set; }
        public string GeneralSupplierLabel { get; set; }
    }
}
