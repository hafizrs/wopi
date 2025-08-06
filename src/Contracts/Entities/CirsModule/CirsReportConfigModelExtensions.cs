using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Client;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.CirsModule;

#nullable enable
public static class CirsReportConfigModelExtensions
{
    public static AssignmentLevel GetAssignmentLevel(
        this CirsReportConfigModel cirsReportConfig,
        CirsDashboardName dashboardName)
    {
        var assignmentLevelStr = dashboardName switch
        {
            CirsDashboardName.Incident => cirsReportConfig.Cirs,
            CirsDashboardName.Complain => cirsReportConfig.ComplainManagement,
            CirsDashboardName.Hint => cirsReportConfig.HintManagement,
            CirsDashboardName.Another => cirsReportConfig.AnotherMessage,
            CirsDashboardName.Idea => cirsReportConfig.IdeaManagement,
            CirsDashboardName.Fault => cirsReportConfig.FaultManagement,
            _ => string.Empty
        };

        return assignmentLevelStr.EnumValue<AssignmentLevel>();
    }
}
