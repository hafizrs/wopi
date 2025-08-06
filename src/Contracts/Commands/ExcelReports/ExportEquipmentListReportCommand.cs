using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports
{
    public class ExportEquipmentListReportCommand : ExportReportCommand
    {
        public bool EnableDateRange { get; set; }
        public TranslationEpuimentList Translation { get;set;}
    }
}
