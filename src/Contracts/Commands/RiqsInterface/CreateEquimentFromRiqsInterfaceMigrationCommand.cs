using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.RiqsInterface
{
    public class CreateEquimentFromRiqsInterfaceMigrationCommand
    {
        public List<string> EquipmentIds { get; set; }
        public string MigrationSummaryId { get; set; }
    }
}
 