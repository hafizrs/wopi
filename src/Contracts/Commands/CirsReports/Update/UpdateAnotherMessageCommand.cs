using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Update;

#nullable enable
public class UpdateAnotherMessageCommand : AbstractUpdateCirsReportCommand
{
    public string? ResponseText { get; set; }
    public string ReporterClientId { get; set; } = null!;
    public OriginatorInfo? OriginatorInfo { get; set; }
    public string? ImplementationProposal { get; set; }
    public override CirsDashboardName CirsDashboardName => CirsDashboardName.Another;
}
