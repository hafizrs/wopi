using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Text;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class RiqsShift: EntityBase
    {
        public string ShiftName { get; set; }
        public string DepartmentId { get; set; }
        public string OrganizationId { get; set; }
        public int Sequence { get; set; }
        public List<string> PraxisFormIds { get; set; }
        public List<PraxisDocument> Files { get; set; }
        public List<PraxisLibraryEntityDetail> LibraryForms { get; set; }
        public List<PraxisLibraryFormResponse> LibraryFormResponses { get; set; }
    }
}
