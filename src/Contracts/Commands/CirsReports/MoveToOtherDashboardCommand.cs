using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports;

#nullable enable
public class MoveToOtherDashboardCommand
{
    public string CirsReportId { get; set; } = null!;

    public string NewDashboardName { get; set; } = null!;

    public CirsDashboardName NewDashboardNameEnum => NewDashboardName.EnumValue<CirsDashboardName>();
}
