using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class UpdateShiftPlanCommand
    {
        public List<string> ShiftPlanIds { get; set; } = new List<string>();
        public List<string> PraxisUserIds { get; set; }
    }
}
