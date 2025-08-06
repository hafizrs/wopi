namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports;

#nullable enable
public class ActiveInactiveCirsReportCommand
{
    public string CirsReportId { get; set; } = null!;

    public bool MarkAsActive { get; set; }
}
