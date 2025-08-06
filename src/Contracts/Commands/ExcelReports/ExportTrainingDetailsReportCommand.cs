using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports
{
    public class ExportTrainingDetailsReportCommand : ExportReportCommand
    {
        public string TrainingId { get; set; }
        public TrainingDetailsTranslation Translation { get; set; }
    }
}
