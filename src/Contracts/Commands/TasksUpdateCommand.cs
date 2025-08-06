using System.Collections.Generic;
using System.Text.Json;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class TasksUpdateCommand
    {
        public string[] TaskScheduleIds { get; set; }
        public string NotificationSubscriptionId { get; set; }
        public bool HasTaskScheduleIntoRelatedEntity { get; set; }
        public RelatedEntityObjectModel RelatedEntityObject { get; set; }
    }

    public class RelatedEntityObjectModel
    {
        public bool IsActive { get; set; }
    }
}