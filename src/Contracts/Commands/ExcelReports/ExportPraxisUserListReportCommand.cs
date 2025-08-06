using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.ExcelReports
{
    public class ExportPraxisUserListReportCommand : ExportReportCommand
    {
        public string ReportName { get; set; }
        public string ClientName { get; set; }
        public Dictionary<string, string> PraxisRolesLookup { get; set; }
    }
}