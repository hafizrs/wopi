using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Create;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;
using System.Collections.Generic;

#nullable enable
namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports.Update;

public class UpdateComplainReportCommand : AbstractUpdateCirsReportCommand
{
    public string? ResponseText { get; set; }
    public OriginatorInfo? OriginatorInfo { get; set; } = null;
    public override CirsDashboardName CirsDashboardName => CirsDashboardName.Complain;
}