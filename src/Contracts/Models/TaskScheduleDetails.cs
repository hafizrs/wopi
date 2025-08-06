using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class TaskScheduleDetails
    {
        public bool HasToMoveNextDay { get; set; }
        public bool IsRepeat { get; set; }
        public List<string> SubmissionDates { get; set; }
    }
}
