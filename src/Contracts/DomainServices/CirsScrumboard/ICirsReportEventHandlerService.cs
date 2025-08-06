using Selise.Ecap.SC.PraxisMonitor.Contracts.Models.CirsReport;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.CirsScrumboard
{
    public interface ICirsReportEventHandlerService
    {
        Task ProcessEmailForCirsExternalUsers(CirsReportEvent cirsReportEvent);
    }
}
