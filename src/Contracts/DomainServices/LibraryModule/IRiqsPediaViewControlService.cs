using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices
{
    public interface IRiqsPediaViewControlService
    {
        Task<RiqsPediaViewControlResponse> GetRiqsPediaViewControl();
        Task UpsertRiqsPediaViewControl(UpsertRiqsPediaViewControlCommand command);
    }
}
