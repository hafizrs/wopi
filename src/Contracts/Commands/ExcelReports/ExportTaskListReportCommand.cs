using System;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports
{
    public class ExportTaskListReportCommand : ExportReportCommand
    {
        public bool EnableDateRange { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ExportTaskListReportTranslation Translation { get; set; }
    }

    public class ExportTaskListReportTranslation
    {
        public string REPORT_NAME { get; set; }
        public string DATE { get; set; }
        public string ORGANIZATION { get; set; }
        public string RESPONSIBLE_PERSON { get; set; }
        public string CATEGORY { get; set; }
        public string SUB_CATEGORY { get; set; }
        public string TASK { get; set; }
        public string TASK_MONITOR_REPORT { get; set; }
        public string SCORE { get; set; }
        public string REMARK { get; set; }
        public string DATE_TIME { get; set; }
        public string CONTROLLING_PERSON { get; set; }
        public string SUBMISSION_DATE { get; set; }
    }
}
