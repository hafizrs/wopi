using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.AbsenceModule
{
    public class DeleteAbsencePlanCommand
    {
        public List<string> ItemIds { get; set; }
    }
}