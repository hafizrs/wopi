using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands.CirsReports;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumBoard;

public interface IActiveInactiveCirsReportService
{
    Task InitiateActiveInactiveAsync(ActiveInactiveCirsReportCommand command);
}
