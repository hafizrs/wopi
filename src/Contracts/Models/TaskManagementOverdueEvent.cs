using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class TaskManagementOverdueEvent
    {
        public TaskSchedule TaskSchedule { get; set; }
        public string PersonId { get; set; }
    }
}
