using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule
{
    public class GenerateQuickTaskReportCommand : ExportReportCommand
    {
        public string SearchText { get; set; }
    }
} 