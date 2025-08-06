using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;

public class CreateFaultReportCommand : AbstractCreateCirsReportCommand
{
    public override CirsDashboardName CirsDashboardName => CirsDashboardName.Fault;
}