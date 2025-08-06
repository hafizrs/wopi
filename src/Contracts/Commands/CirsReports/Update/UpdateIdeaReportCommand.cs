#nullable enable
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Update;

public class UpdateIdeaReportCommand : AbstractUpdateCirsReportCommand
{
    public string? ReporterClientId { get; set; }

    public string? BenefitOfIdea { get; set; }

    public string? TargetGroup { get; set; }

    public string? FeasibilityAndResourceRequirements { get; set; }

    public string? Requirements { get; set; }
    public string? DecisionSelection { get; set; }
    public string? DecisionSelectionReason { get; set; }

    public override CirsDashboardName CirsDashboardName => CirsDashboardName.Idea;
}
