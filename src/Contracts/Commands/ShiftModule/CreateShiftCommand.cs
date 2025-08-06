using System;
using System.Collections.Generic;
using System.Text;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class CreateShiftCommand
    {
        public string ShiftName { get; set; }
        public string DepartmentId { get; set; }
        public List<string> PraxisFormIds { get; set; }
        public int Sequence { get; set; }
        public List<PraxisDocument> Files { get; set; } = new List<PraxisDocument>();
        public List<PraxisLibraryEntityDetail> LibraryForms { get; set; } = new List<PraxisLibraryEntityDetail>();
    }
}
