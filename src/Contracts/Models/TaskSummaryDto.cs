using System;
using System.Collections.Generic;
using Selise.Ecap.Entities.PrimaryEntities.TaskManagement;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class TaskSummaryDto
    {
        public string ItemId { get; set; }
        public DateTime FromDate { get; set; }
        public string FromTime { get; set; }
        public DateTime ToDate { get; set; }
        public string ToTime { get; set; }
        public bool IsRepeat { get; set; }
        public int? RepeatValue { get; set; }
        public int? RepeatAfterOccurence { get; set; }
        public int RepeatType { get; set; }
        public bool ReserveForEver { get; set; }
        public DateTime? StartsOnDate { get; set; }
        public DateTime? RepeatEndDate { get; set; }
        public int? RepeatEndOption { get; set; }
        public IEnumerable<int> RepeatOnDayOfWeeks { get; set; }
        public bool IsDayOfTheMonth { get; set; }
        public IEnumerable<DateTime> SubmissionDates { get; set; }
        public IEnumerable<RepeatingDateProp> RepeatingDates { get; set; }
        public bool Status { get; set; }
        public bool AvoidWeekEnd { get; set; }
        public bool IsMoveUncheckedTask { get; set; }
        public bool IsTaskReScheduled { get; set; }
    }
}
