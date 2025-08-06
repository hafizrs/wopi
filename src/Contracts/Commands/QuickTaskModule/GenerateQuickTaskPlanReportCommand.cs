using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.ShiftPlan;
using System;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.QuickTaskModule
{
    public class GenerateQuickTaskPlanReportCommand : ExportReportCommand
    {
        public DateTime StartDate { get; set; }
        public ShiftPlanReportType ViewMode { get; set; }
    }
} 