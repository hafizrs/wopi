using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports;

#nullable enable
public class AssignCirsAdminsCommand : AddRemoveCommand
{
    public string OrganizationId { get; set; } = null!;

    public string PraxisClientId { get; set; } = null!;

    public string DashboardName { get; set; } = null!;

    public CirsDashboardName DashboardNameEnum => DashboardName.EnumValue<CirsDashboardName>();
}
