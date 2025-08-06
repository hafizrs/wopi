using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Client;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface
{
    public class UpdateEquipemtInterfaceAdditioanalDataCommand
    {
        [Required]
        public List<string> EquipmentIds { get; set; }
        [Required]
        public string MigrationSummaryId { get; set; }
        public List<PraxisDocument> Attachments { get; set; }
        public IEnumerable<PraxisKeyValue> MetaValues { get; set; }
        public EquipmentSupplier Supplier { get;  set; }
        public EquipmentLocation Location { get; set; }
        public EquipmentManufacturer Manufacturer { get; set; }



    }

    public class EquipmentSupplier
    {
        public string SupplierId { get; set; }
        public string SupplierName { get; set; }
    }

    public class EquipmentLocation
    {
        public string RoomId { get; set; }
        public string RoomName { get; set; }
    }

    public class EquipmentManufacturer
    {
        public string Manufacturer { get; set; }
        public string ManufacturerId { get; set; }
    }
  
}
