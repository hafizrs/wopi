#nullable enable
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;

public class CreateIdeaReportCommand : AbstractCreateCirsReportCommand
{
    public string ReporterClientId { get; set; } = null!;

    public string? BenefitOfIdea { get; set; } = null;

    public string? TargetGroup { get; set; } = null;

    public string? FeasibilityAndResourceRequirements { get; set; } = null;

    public string? Requirements { get; set; } = null;

    public override CirsDashboardName CirsDashboardName => CirsDashboardName.Idea;
}
