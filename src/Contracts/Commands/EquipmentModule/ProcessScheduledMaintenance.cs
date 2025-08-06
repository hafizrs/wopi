using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class ProcessScheduledMaintenance
    {
        public string MaintenanceId { get; set; }
        public PraxisKeyValue CompletionStatus { get; set; }
        public EquipmentMaintenanceAnswer MaintenanceAnswer { get; set; }
        public bool ForExternalUser { get; set; }
    }
}
