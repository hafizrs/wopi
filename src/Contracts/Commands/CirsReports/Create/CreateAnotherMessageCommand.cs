using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;

public class CreateAnotherMessageCommand : AbstractCreateCirsReportCommand
{
    public string ReporterClientId { get; set; } = null!;
    public InvolvedUser ReportedBy { get; set; }
    public OriginatorInfo OriginatorInfo { get; set; } = null;
    public string ImplementationProposal { get; set; }
    public override CirsDashboardName CirsDashboardName => CirsDashboardName.Another;
}
