using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models.QuickTaskModule
{
    public class RiqsQuickTask : EntityBase
    {
        public string TaskGroupName { get; set; }
        public List<RiqsSingleQuickTask> TaskList { get; set; }
        public string DepartmentId { get; set; }
        public string OrganizationId { get; set; }
        public int Sequence { get; set; }
    }
}