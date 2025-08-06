namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports
{
    public class ExportProcessGuideCaseOverviewReportCommand : ExportReportCommand
    {
        public string ReportHeader { get; set; }
        public bool IsShiftPlan { get; set; }
    }
}