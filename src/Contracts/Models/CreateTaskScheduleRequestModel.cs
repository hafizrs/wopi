using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class CreateTaskScheduleRequestModel
    {
        public TaskScheduleDetails TaskScheduleDetails { get; set; }
        public List<TaskData> TaskDatas { get; set; }
        public List<object> AssignMembers { get; set; }
        public string NotificationSubscriptionId { get; set; }
    }
}
