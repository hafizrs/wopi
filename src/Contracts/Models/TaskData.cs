using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class TaskData
    {
        public string TaskSummaryId { get; set; }
        public bool HasRelatedEntity { get; set; }
        public string Title { get; set; }
        public string RelatedEntityName { get; set; }
        public RelatedEntityObject RelatedEntityObject { get; set; }
        public bool HasTaskScheduleIntoRelatedEntity { get; set; }
    }
}
