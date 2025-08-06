using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports
{
    public class ExportTrainingReportCommand: ExportReportCommand
    {
        public TrainingReportTranslation Translation { get; set; }
    }
}
