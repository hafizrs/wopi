using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.QuickTaskModule;
using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using SeliseBlocks.Genesis.Framework.PDS.Entity;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    public class RiqsQuickTaskResponse: EntityBase
    {
        public RiqsQuickTaskResponse() { }
        public RiqsQuickTaskResponse(RiqsQuickTask quickTask)
        {
            ItemId = quickTask.ItemId;
            TaskGroupName = quickTask.TaskGroupName;
            TaskList = quickTask.TaskList;
            DepartmentId = quickTask.DepartmentId;
            OrganizationId = quickTask.OrganizationId;
            Sequence = quickTask.Sequence;
        }

        public string TaskGroupName { get; set; }
        public List<RiqsSingleQuickTask> TaskList { get; set; }
        public string DepartmentId { get; set; }
        public string OrganizationId { get; set; }
        public int Sequence { get; set; }
    }
}