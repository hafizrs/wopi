using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;

public class GenerateShiftReportCommand : ExportReportCommand
{
    public string SearchText { get; set; }
}