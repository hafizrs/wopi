using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models.QuickTaskModule
{
    public class RiqsSingleQuickTask : EntityBase
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public List<AttachedFormInfo> AttachedFormInfos { get; set; }
        public List<PraxisDocument> Files { get; set; }
        public List<PraxisLibraryEntityDetail> LibraryForms { get; set; }
        public List<PraxisLibraryFormResponse> LibraryFormResponses { get; set; }
        public string DepartmentId { get; set; }
        public string OrganizationId { get; set; }
    }
} 