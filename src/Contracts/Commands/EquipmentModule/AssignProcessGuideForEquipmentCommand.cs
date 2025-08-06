using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.EquipmentModule
{
    public class AssignProcessGuideForEquipmentCommand
    {
        public List<string> FormIds { get; set; }
        public string EquipmentId { get; set; }
    }
}