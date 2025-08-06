using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants.CirsReport;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;

public interface ICirsDashboardUpdateService
{
    Task MoveToOtherDashboardAync(
        string cirsReportId,
        CirsDashboardName newDashboardName);
}
