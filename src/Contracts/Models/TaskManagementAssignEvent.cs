using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class TaskManagementAssignEvent
    {
        public TaskSummary TaskSummary { get; set; }
        public string PersonId { get; set; }
    }
}