namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports
{
    public class ExportProcessGuideReportForDeveloperCommand : ExportReportCommand
    {
        public string ClientName { get; set; }
        public bool IsReportForAllData { get; set; }
        public string ReportHeader { get; set; }
    }
}
