using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Update;

public class UpdateFaultReportCommand : AbstractUpdateCirsReportCommand
{
    public override CirsDashboardName CirsDashboardName => CirsDashboardName.Fault;
}