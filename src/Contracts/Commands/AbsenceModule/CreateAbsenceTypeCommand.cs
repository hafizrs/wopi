using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.AbsenceModule;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.AbsenceModule
{
    public class CreateAbsenceTypeCommand
    {
        public List<AbsenceTypeData> AbsenceTypes { get; set; } = new List<AbsenceTypeData>();
    }

    public class AbsenceTypeData
    {
        public string Type { get; set; }
        public string Color { get; set; }
        public string DepartmentId { get; set; }
    }
}