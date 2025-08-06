namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class TaskDataBasicDto
    {
        public string TaskSummaryId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool HasRelatedEntity { get; set; }
        public string RelatedEntityName { get; set; }
        public bool HasTaskScheduleIntoRelatedEntity { get; set; }
    }

    public class TaskDataDto<T> : TaskDataBasicDto
    {
        public T RelatedEntityObject { get; set; }
    }
}
