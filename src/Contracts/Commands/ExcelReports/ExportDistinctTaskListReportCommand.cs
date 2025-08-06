using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports
{
    public class ExportDistinctTaskListReportCommand : ExportReportCommand
    {
        public TranslationDistinctTaskList Translation { get; set; }
    }
}
