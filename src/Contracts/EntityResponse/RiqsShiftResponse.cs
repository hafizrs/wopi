using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    public class RiqsShiftResponse 
    {
        public RiqsShiftResponse() { }
        public RiqsShiftResponse(RiqsShift riqsShift)
        {
            ItemId = riqsShift.ItemId;
            ShiftName = riqsShift.ShiftName;
            PraxisFormIds = riqsShift.PraxisFormIds;
            Sequence = riqsShift.Sequence;
            Files = riqsShift.Files;
            LibraryForms = riqsShift.LibraryForms;
            LibraryFormResponses = riqsShift.LibraryFormResponses;
        }

        public string ItemId { get; set; }
        public string ShiftName { get; set; }
        public List<PraxisFormResponse> PraxisForms { get; set; }
        public List<string> PraxisFormIds { get; set; }
        public int Sequence { get; set; }
        public List<PraxisDocument> Files { get; set; }
        public List<PraxisLibraryEntityDetail> LibraryForms { get; set; }
        public List<PraxisLibraryFormResponse> LibraryFormResponses { get; set; }
    }
    public class PraxisFormResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
