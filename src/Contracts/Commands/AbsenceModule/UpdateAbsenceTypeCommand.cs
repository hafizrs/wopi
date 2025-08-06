using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.AbsenceModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.AbsenceModule
{
    public class UpdateAbsenceTypeCommand
    {
        public List<AbsenceTypeUpdateData> AbsenceTypes { get; set; } = new List<AbsenceTypeUpdateData>();
    }

    public class AbsenceTypeUpdateData
    {
        public string ItemId { get; set; }
        public string Type { get; set; }
        public string Color { get; set; }
    }
}