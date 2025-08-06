using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Client;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.EquipmentModule;

public class EquipmentCompanyContactInfo : PraxisSupplierContactPerson
{
    public string CompanyId { get; set; }
    public string CompanyName { get; set; }
}