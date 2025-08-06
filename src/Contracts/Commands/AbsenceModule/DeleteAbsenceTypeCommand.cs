using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.AbsenceModule
{
    public class DeleteAbsenceTypeCommand
    {
        public List<string> ItemIds { get; set; } = new List<string>();
    }
}