using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ShiftPlan;
using System;
using System.Collections.Generic;
using System.Text;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands
{
    public class GenerateShiftPlanReportCommand : ExportReportCommand
    {
        public DateTime StartDate { get; set; }
        public ShiftPlanReportType ViewMode { get; set; }

    }
}
