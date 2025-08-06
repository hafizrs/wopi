using System;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports
{
    public class ExportOpenItemReportCommand : ExportReportCommand
    {
        public bool EnableDateRange { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TranslationOpenItem Translation { get; set; }
    }
}