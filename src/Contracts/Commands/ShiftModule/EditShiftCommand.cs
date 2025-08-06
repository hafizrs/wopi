using System;
using System.Collections.Generic;
using System.Text;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class EditShiftCommand
    {
        public string ItemId { get; set; }
        public string ShiftName { get; set; }
        public List<string> PraxisFormIds { get; set; }
        public List<PraxisDocument> Files { get; set; }
        public List<PraxisLibraryEntityDetail> LibraryForms { get; set; }
    }
}
