namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Constants
{
    public class TaskManagementEventType
    {
        protected TaskManagementEventType() { }

        public const string TaskAssignEvent = "TaskManagement.Assign.Summary";
        public const string TaskOverdueEvent = "TaskManagement.TaskSchedule.Overdue";
        public const string TaskScheduleUpdateEvent = "TaskManagement.TaskSchedule.Update";
    }
}
