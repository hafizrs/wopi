namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports
{
    public class ExportProcessGuideDetailReportCommand : ExportReportCommand
    {
        public string ProcessGuideId { get; set; }
        public string ReportHeader { get; set; }
        public string ClientName { get; set; }
    }
}
