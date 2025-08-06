using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports;

public class DeleteCirsReportsCommand
{
    public List<string> CirsReportIds { get; set; }
}