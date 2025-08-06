using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.AbsenceModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.AbsenceModule
{
    public class UpdateAbsencePlanStatusCommand
    {
        public string ItemId { get; set; }
        public AbsencePlanStatus Status { get; set; }
        public string ReasonToDeny { get; set; }
    }
}