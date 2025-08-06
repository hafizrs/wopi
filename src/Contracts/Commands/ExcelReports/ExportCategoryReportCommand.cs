using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports
{
    public class ExportCategoryReportCommand: ExportReportCommand
    {
        public CategoryReportTranslation Translation { get; set; }
    }
}
