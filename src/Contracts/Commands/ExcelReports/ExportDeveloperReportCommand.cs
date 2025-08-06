using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports
{
    public class ExportDeveloperReportCommand : ExportReportCommand
    {
        public bool IsReportForAllData { get; set; }
        public DeveloperReportTranslation Translation { get; set; }
    }
}
