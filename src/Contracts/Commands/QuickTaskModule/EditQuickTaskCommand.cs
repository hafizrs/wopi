using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.LibraryModule;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.QuickTaskModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule
{
    public class EditQuickTaskCommand
    {
        public string ItemId { get; set; }
        public string TaskGroupName { get; set; }
        public List<RiqsSingleQuickTask> TaskList { get; set; }
        public string DepartmentId { get; set; }
        public string OrganizationId { get; set; }
    }
} 